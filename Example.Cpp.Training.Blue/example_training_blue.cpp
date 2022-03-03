/**
 * @file example_training_blue.cpp
 * @brief Example demonstrating the API of the ViDi training-library
 *
 * This example demonstrates how to train a blue tool using the ViDi API.
 */

#include "vidi.h"
#include "vidi_training.h"

#include<string>
#include<iostream>

// RapidXML is a lightweight, header only, fast xml parser.
#define USE_RAPIDXML
#ifdef USE_RAPIDXML
#include "../include/rapidxml/rapidxml.hpp"
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
    {
        return "failed to get last error message";
    }

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
const std::string resources_path("..\\resources\\images\\");

//where the workspace will be created
std::string working_path(".\\ws\\");

// Image list from the Screws tutorial
const char* image_list[] = { "good_001.png", "good_002.png", "good_003.png", "good_004.png" };

size_t image_list_len = sizeof(image_list) / sizeof(image_list[0]);

const char* workspace_name = "screws";
const char* stream_name = "default";
const char* tool_name = "localize";

/**
 * @brief Example application illustrating the utilisation of the ViDi training-library
 *
 * This example illustrates how to :
 * create a new workspace
 * add a blue tool
 * label the images
 * create and modify a model
 *  trains the tool
 * export the resulting workspace.
 */
int main(int argc, char* argv[])
{
    VIDI_UINT status = vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "");
    if (status != VIDI_SUCCESS)
    {
        std::cerr << "failed to initialize library" << std::endl;
        return -1;
    }

    //Initialize a buffer that will be used through the complete example
    VIDI_BUFFER buffer;
    status = vidi_init_buffer(&buffer);
    CHECK_STATUS(status);

    std::string workspace_path = working_path + workspace_name;
    // creates a new workspace
    // the path has to be empty when the workspace is created
    status = vidi_training_create_workspace(workspace_name, workspace_path.c_str());
    CHECK_STATUS(status);

    // adds a stream "default" to the workspace
    status = vidi_training_workspace_add_stream(workspace_name, stream_name);
    CHECK_STATUS(status);

    // adds a blue tool to the stream, "default", at the beginning of the tool chain
    status = vidi_training_stream_add_tool(workspace_name, stream_name, tool_name, "", "blue");
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

    // Process the database to set the ROI of the images
    status = vidi_training_tool_process_database(workspace_name, stream_name, tool_name, "", "");
    CHECK_STATUS(status);

    // Waits for the processing to be finished
    status = vidi_training_tool_wait(workspace_name, stream_name, tool_name, 0);
    CHECK_STATUS(status);

    /*
    * Change training parameters and model parameters
    */

    // get feature size value
    status = vidi_training_tool_get_parameter(workspace_name, stream_name, tool_name, "sampling/feature_size", &buffer);
    CHECK_STATUS(status);

    // change feature size
    std::cout << "setting the feature size from " << buffer.data << " to 120x120" << std::endl;
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "sampling/feature_size", "120x120");
    CHECK_STATUS(status);

    // change count_epochs
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "training/count_epochs", "5");
    CHECK_STATUS(status);

    // change_train selection to 50%
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "training/train_selection", "0.5");
    CHECK_STATUS(status);

    // label the first 4 images
    status = vidi_training_blue_set_feature(workspace_name, stream_name, tool_name, "good_001.png:0", -1, "0", 480, 150, 0, 0);
    CHECK_STATUS(status);
    status = vidi_training_blue_set_feature(workspace_name, stream_name, tool_name, "good_001.png:0", -1, "1", 1159, 156, 0, 0);
    CHECK_STATUS(status);
    status = vidi_training_blue_set_feature(workspace_name, stream_name, tool_name, "good_002.png:0", -1, "0", 480, 234, 0, 0);
    CHECK_STATUS(status);
    status = vidi_training_blue_set_feature(workspace_name, stream_name, tool_name, "good_002.png:0", -1, "1", 1156, 235, 0, 0);
    CHECK_STATUS(status);

    // create 2-node model with name "screw"
    status = vidi_training_blue_create_model(workspace_name, stream_name, tool_name, "screw", 0);
    CHECK_STATUS(status);
    status = vidi_training_model_add_node(workspace_name, stream_name, tool_name, "screw");
    CHECK_STATUS(status);
    status = vidi_training_model_add_node(workspace_name, stream_name, tool_name, "screw");
    CHECK_STATUS(status);

    // change threshold of model
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "models/screw/threshold", "0.6");
    CHECK_STATUS(status);

    // set position and ID of model nodes
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "models/screw/nodes/0/names", "0");
    CHECK_STATUS(status);
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "models/screw/nodes/0/position", "-0.5,0");
    CHECK_STATUS(status);
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "models/screw/nodes/1/names", "1");
    CHECK_STATUS(status);
    status = vidi_training_tool_set_parameter(workspace_name, stream_name, tool_name, "models/screw/nodes/1/position", "0.5,0");
    CHECK_STATUS(status);

    // trains the workspace without specifying the GPU
    status = vidi_training_tool_train(workspace_name, stream_name, tool_name, "");
    CHECK_STATUS(status);

    std::cout << "blocks and waits until the training is done" << std::endl;
    vidi_training_tool_wait(workspace_name, stream_name, tool_name, 0);

    /*
     * At this point the training has finished, but not necessarly succeeded.
     * To insure that, vidi_training_tool_get_status has to be called.
     * Look at example_training_red for how to check the status.
     */

    std::cout << "training done" << std::endl;


    /// Exports the workspace with the images to a vidi workspace archive
    status = vidi_training_export_workspace_to_file(workspace_name, (std::string(workspace_name) + ".vwsa").c_str(), 1);
    CHECK_STATUS(status);

    // saves the workspace to the disk
    status = vidi_training_save_workspace(workspace_name, 0);
    CHECK_STATUS(status);

    // closes the workspace
    status = vidi_training_close_workspace(workspace_name, 0);
    CHECK_STATUS(status);

    // uncomment it if you want the system to wait for a user input before exiting
    //system("pause");

    // this will free all images and buffers which have not yet been freed
    vidi_deinitialize();

    return 0;
}
