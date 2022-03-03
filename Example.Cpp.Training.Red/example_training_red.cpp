/**
 * @file example_training_red.cpp
 * @brief Example demonstrating the API of the ViDi training-library
 *
 * This example demonstrates how to train a red tool using the ViDi API.
 */

#include "vidi.h"
#include "vidi_training.h"
#include<string>
#include<iostream>

// RapidXML is a lightweight, header only, fast xml parser.
#define USE_RAPIDXML
#ifdef USE_RAPIDXML
    #include "../include/rapidxml/rapidxml.hpp"
    #include <sstream>
    #include <iomanip>
#endif


/**
 * @brief gets the error message from the status using VIDI_BUFFER
 *
 * @return a string containing the last error message or a string indicating that we couldn't get the last error message
 */
std::string get_last_error_message(int status)
{
    VIDI_BUFFER buffer;
    vidi_init_buffer(&buffer);

    VIDI_UINT internal_status;

    internal_status = vidi_get_error_message(status, &buffer);
    if (internal_status != VIDI_SUCCESS)
        return "failed to get last error message";

#ifdef USE_RAPIDXML
    rapidxml::xml_document<> doc;
    doc.parse<0>(buffer.data);

    std::string error_message(doc.first_node("error")->value());
#else
    std::string error_message = buffer.data;
#endif

    return error_message;
}

/**
* @brief checks if the status passes, prints the last error message and returns the user defined error code
*/
#define CHECK_STATUS(f)                          \
{                                                \
    VIDI_UINT s = f;                             \
    if (s != VIDI_SUCCESS)                       \
    {                                            \
        std::cerr << get_last_error_message(s)   \
        << std::endl; vidi_deinitialize();       \
        return -1;                               \
    }                                            \
}

// where the images are located 
// if do_train = false where the vidi workspace archive is located
const std::string resources_path("..\\resources\\images\\");

//where the workspace will be created
std::string working_path(".\\ws\\");

// Image list from the Textile tutorial
const char* image_list[] = { "000000.png", "000001.png", "000002.png", "000003.png", "000004.png", "000005.png", "000006.png", "000007.png", "000008.png", "000009.png", "000010.png", "000011.png", "000012.png", "000013.png", "000016.png", "000017.png", "000018.png", "bad000001.png", "bad000002.png" };
size_t image_list_len = sizeof(image_list) / sizeof(image_list[0]);

const char* workspace_name = "textile";
const char*  stream_name = "default";
const char*  tool_name = "analyze";

/*
* Example application illustrating the utilisation of the ViDi training-library
*/
int main(int argc, char* argv[])
{
    // initialize the libary
    if (vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "") != VIDI_SUCCESS)
    {
        std::cerr << "failed to initialize library" << std::endl;
        return -1;
    }

    //Initialize a buffer that will be used through the complete example
    VIDI_BUFFER buffer;
    vidi_init_buffer(&buffer);

    std::string workspace_path = working_path + workspace_name;
    // creates a new workspace
    //!\ the path has to be empty when the workspace is created
    VIDI_UINT status = vidi_training_create_workspace(workspace_name, workspace_path.c_str());
    CHECK_STATUS(status);

    // adds a stream "default" to the workspace
    status = vidi_training_workspace_add_stream(workspace_name, stream_name);
    CHECK_STATUS(status);

    // adds a red tool to the stream default at the beginning of the tool chain
    status = vidi_training_stream_add_tool(workspace_name, stream_name, tool_name, "", "red");
    CHECK_STATUS(status);

    std::cout << "will add " << image_list_len << " images to the database" << std::endl;

    // Loop over all the images in image list and uplaod them to database
    VIDI_IMAGE img;
    status = vidi_init_image(&img);
    CHECK_STATUS(status);
    for (size_t img_idx = 0; img_idx < image_list_len; ++img_idx)
    {
        std::string filename = resources_path + image_list[img_idx];

        status = vidi_load_image(filename.c_str(), &img);
        CHECK_STATUS(status);

        status = vidi_training_stream_add_image_to_database(workspace_name, stream_name, &img, image_list[img_idx]);
        CHECK_STATUS(status);
    }
    status = vidi_free_image(&img);
    CHECK_STATUS(status);

    std::cout << "successfully added " << image_list_len << " images to the database" << std::endl;

    /*
    * Tags all images containing 'bad' in the filename as bad, good otherwise
    */
    std::cout << "will tag the database" << std::endl;

    // Process the database to set the ROI of the images
    status = vidi_training_tool_process_database(workspace_name, stream_name, tool_name, "", "");
    CHECK_STATUS(status);

    // Waits for the processing to be finished
    status = vidi_training_tool_wait(workspace_name, stream_name, tool_name, 0);
    CHECK_STATUS(status);

    // Labels the views containing 'bad' as Bad
    status = vidi_training_red_label_views(workspace_name, stream_name, tool_name, "'bad'", "Bad");
    CHECK_STATUS(status);

    // Labels all other views (not labeled) as Good ("")
    status = vidi_training_red_label_views(workspace_name, stream_name, tool_name, "not labeled", "");
    CHECK_STATUS(status);

    /*
    * Trains the workspace and wait for the training to be finished
    */

    // get feature size value
    status = vidi_training_tool_get_parameter(workspace_name, stream_name, tool_name, "sampling/feature_size", &buffer);
    CHECK_STATUS(status);

    std::cout << "setting the feature size from " << buffer.data << " to 150x150" << std::endl;

    // sets feature size value
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "sampling/feature_size", "150x150");
    CHECK_STATUS(status);

    // change count_epochs
    status = vidi_training_tool_get_parameter(workspace_name, stream_name, tool_name, "training/count_epochs", &buffer);
    CHECK_STATUS(status);
    int count_epochs = atoi(buffer.data);
    std::cout << "setting the number of training epochs from " << count_epochs << " to 10" << std::endl;
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "training/count_epochs", "10");
    CHECK_STATUS(status);

    // trains the workspace without specifying the GPU
    status = vidi_training_tool_train(workspace_name, stream_name, tool_name, "");
    CHECK_STATUS(status);


#ifdef USE_RAPIDXML
    bool busy = true;
    while (busy)
    {
        // waits 1 second for the training to be finished
        status = vidi_training_tool_wait(workspace_name, stream_name, tool_name, 1000);
        CHECK_STATUS(status);


        /**
        * the returned status gives us information about errors, whether the tool needs training,
        * whether it's loaded, and whether it's ready. The status will also give us information
        * about the progress.
        */
        status = vidi_training_tool_get_status(workspace_name, stream_name, tool_name, &buffer);
        CHECK_STATUS(status);

        // Parse the status and outputs the description to stdout
        // Also gets the state : if ready = true the training is finished
        // and check the response contains a string in the attribute error

        rapidxml::xml_document<> doc;
        doc.parse<0>(buffer.data);

        rapidxml::xml_node<> * status_node = doc.first_node("status");

        // we check if there are any errors reported by the status
        if (rapidxml::xml_attribute<> * error_attribute = status_node->first_attribute("error"))
        {
            std::string error_message(error_attribute->value());
            if (!error_message.empty())
            {
                std::cerr << error_message;
                return -1;
            }
        }

        // we check the needs_training attribute to tell us when to stop waiting and leave this loop
        std::istringstream(status_node->first_attribute("busy")->value()) >> std::boolalpha >> busy;
        rapidxml::xml_node<> * progress_node = status_node->first_node("progress");

        std::string description = progress_node->value();

        std::cout << "\r" << description << std::setw(20) << "";
        std::cout.flush();
    }
    std::cout << std::endl;
#else
    std::cout << "blocks and waits until the training is done" << std::endl;
    vidi_training_tool_wait(workspace_name, stream_name, tool_name, 0);
#endif
    /*
     * At this point the training has finished, but not necessarly succeeded.
     * To ensure that, vidi_training_tool_get_status has to be called. If you're
     * using rapidxml, any errors would have been caught above.
     */

    std::cout << "training done" << std::endl;

    /*
    * Exports the workspaces
    */

    // Exports the workspace with the images to a vidi workspace archive
    status = vidi_training_export_workspace_to_file(workspace_name, (std::string(workspace_name) + ".vwsa").c_str(), 1);
    CHECK_STATUS(status);
    // Exports the runtime workspace
    status = vidi_training_export_runtime_workspace_to_file(workspace_name, (std::string(workspace_name) + ".vrws").c_str());
    CHECK_STATUS(status);

    /*
    * Saves and close the workspace
    */

    // saves the workspace to the disk
    status = vidi_training_save_workspace(workspace_name, 0);
    CHECK_STATUS(status);

    // closes the workspace, discarding auto saves
    status = vidi_training_close_workspace(workspace_name, 1);
    CHECK_STATUS(status);

    // uncomment it if you want the system to wait for a user input before exiting
    //system("pause");

    // this will free all images and buffers which have not yet been freed
    vidi_deinitialize();

    return 0;
}
