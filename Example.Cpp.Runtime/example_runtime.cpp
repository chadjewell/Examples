/**
 * @file example_runtime.cpp
 * @brief Example demonstrating how to process a sample
 */

#include "vidi_runtime.h"

#include <iostream>
#include <fstream>

using namespace std;

/**
 * @brief demonstrate the API for processing a sample in a given workspace
 *
 * this example uses the API that's new to 3.0.0. This breaks the process
 * step into many smaller steps. 
 */
int main()
{
    // send debug info to a message file
    VIDI_UINT status = vidi_debug_infos(VIDI_DEBUG_SINK_FILE, "vidi_messages.log");
    if (status != VIDI_SUCCESS)
    {
        std::cerr << "failed to enable debug infos" << endl;
        return -1;
    }

    // initialize the libary to run with one GPU per tool
    status = vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "");
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize library" << endl;
        return -1;
    }

    // create and initialize a buffer to be used whenever data is returned from the library
    VIDI_BUFFER buffer;
    status = vidi_init_buffer(&buffer);
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize vidi buffer" << endl;
        return -1;
    }

    // open the given workspace
    status = vidi_runtime_open_workspace_from_file("workspace", "..\\resources\\runtime\\Textile.vrws");
    if (status != VIDI_SUCCESS)
    {
        vidi_get_error_message(status, &buffer);
        // if you get this error, it's possible that the resources were not extracted in the path
        clog << "failed to open workspace: " << buffer.data << endl;
        vidi_deinitialize();
        return -1;
    }

    {
        VIDI_IMAGE image;
        status = vidi_init_image(&image);
        if (status != VIDI_SUCCESS)
        {
            vidi_get_error_message(status, &buffer);
            clog << "failed to initialize image: " << buffer.data << endl;
            vidi_deinitialize();
            return -1;
        }
        status = vidi_load_image("..\\resources\\images\\bad000001.png", &image);
        if (status != VIDI_SUCCESS)
        {
            vidi_get_error_message(status, &buffer);
            // if you get this error, it's possible that the resources were not extracted in the path
            clog << "failed to read image: " << buffer.data << endl;
            vidi_deinitialize();
            return -1;
        }

        VIDI_BUFFER result_buffer;
        status = vidi_init_buffer(&result_buffer);
        if (status != VIDI_SUCCESS)
        {
            clog << "failed to initialize result buffer" << endl;
            vidi_deinitialize();
            return -1;
        }

        // as of ViDi Suite 3.0.0, samples are processed in a few steps instead of just calling vidi_runtime_process
        // the first step is to initialize the sample
        status = vidi_runtime_create_sample("workspace", "default", "my_sample");
        if (status != VIDI_SUCCESS)
        {
            clog << "failed to initialize sample" << endl;
            vidi_deinitialize();
            return -1;
        }

		// then add the image to be processed
		status = vidi_runtime_sample_add_image("workspace", "default", "my_sample", &image);
		if (status != VIDI_SUCCESS)
		{
			clog << "failed to add image" << endl;
			vidi_deinitialize();
			return -1;
		}

        /**
         * subsequently process the sample for each tool
         * here, we know that the tool that's being processed is called "analyze". If you do not know the name of the
         * tool or tools that are being processed, you can use vidi_runtime_list_tools(). Since there is only one tool,
         * this is called only once. You can also trigger the whole chain to process by calling
         * vidi_runtime_process_sample for the last tool in the chain.
         */
        status = vidi_runtime_sample_process("workspace", "default", "analyze", "my_sample", "");
        if (status != VIDI_SUCCESS)
        {
            vidi_get_error_message(status, &buffer);
            clog << "failed to process sample: " << buffer.data << endl;
            vidi_deinitialize();
            return -1;
        }

        // the next step is to get the results
        status = vidi_runtime_get_sample("workspace", "default", "my_sample", &result_buffer);
        if (status != VIDI_SUCCESS)
        {
            clog << "failed to get results from sample" << endl;
            vidi_deinitialize();
            return -1;
        }

        clog << "writing result to 'result.xml'" << endl;
        ofstream ofs("result.xml");
        ofs << result_buffer.data;

        vidi_free_buffer(&result_buffer);
        vidi_free_image(&image);

		// now that we've gotten the results, we can free the sample
		status = vidi_runtime_free_sample("workspace", "default", "my_sample");
		if (status != VIDI_SUCCESS)
		{
			clog << "failed to free sample" << endl;
			vidi_deinitialize();
			return -1;
		}
    }

    // this will free all images and buffers which have not yet been freed
    vidi_deinitialize();

    return 0;
}
