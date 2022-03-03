/**
 * @file example_runtime.cpp
 * @brief Example demonstrating using multiple GPUs with ViDi's runtime library
 */

#include <iostream>
#include <fstream>
#include <string>

#include <thread>
#include <chrono>
#include <vector>

#include "vidi_runtime.h"
#include "../include/rapidxml/rapidxml.hpp"

using namespace std;

struct Device
{
    Device(string n, string i) { name = n; id = i; }
    string name;
    string id;
};

/**
 * @brief Process an image n_iter times using only one GPU
 */
int single_gpu(string image_path, string workspace_path, Device device, size_t n_iter = 100)
{
    VIDI_UINT status;
    status = vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, device.id.c_str());
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize library" << endl;
        return -1;
    }

    // enable debug output to a message file
    vidi_debug_infos(VIDI_DEBUG_SINK_FILE, "vidi_messages.log");

    // create and initialize a buffer to be used whenever data is returned from the library
    VIDI_BUFFER buffer;
    vidi_init_buffer(&buffer);

    // open the given workspace
    status = vidi_runtime_open_workspace_from_file("workspace", workspace_path.c_str());
    if (status != VIDI_SUCCESS)
    {
        vidi_get_error_message(status, &buffer);

        clog << string("failed to open workspace '") << workspace_path << "': " << buffer.data << endl;

        vidi_deinitialize();

        return -1;
    }

    {
        // load the image from the file specified in the command line
        VIDI_IMAGE image;
        vidi_init_image(&image);
        status = vidi_load_image(image_path.c_str(), &image);
        if (status != VIDI_SUCCESS)
        {
            vidi_get_error_message(status, &buffer);

            clog << "failed to read image '" << image_path.c_str() << "': " << buffer.data << endl;

            vidi_deinitialize();

            return -1;
        }

        auto start = std::chrono::system_clock::now();
        for (size_t iter = 0; iter < n_iter; ++iter)
        {
            status = vidi_runtime_create_sample("workspace", "default", "my_sample");
            if (status != VIDI_SUCCESS)
            {
                vidi_deinitialize();
                return -1;
            }

			status = vidi_runtime_sample_add_image("workspace", "default", "my_sample", &image);
			if (status != VIDI_SUCCESS)
			{
				vidi_deinitialize();
				return -1;
			}

            status = vidi_runtime_sample_process("workspace", "default", "analyze", "my_sample", "");
            if (status != VIDI_SUCCESS)
            {
                vidi_deinitialize();
                return -1;
            }

            vidi_runtime_free_sample("workspace", "default", "my_sample");
        }

        auto end = std::chrono::system_clock::now();
        auto elapsed = chrono::duration_cast<std::chrono::milliseconds>(end - start);
        std::cout << "elapsed time using a single GPU: " << elapsed.count() << " ms (average: " << elapsed.count() / n_iter << " ms)" << endl;

        vidi_free_image(&image);
        vidi_deinitialize();
    }
    return 0;
}

/**
* @brief Process an image n_iter times using using multiple GPUs per tool
*/
int multi_gpu_per_tool(string image_path, string workspace_path, std::vector<Device> devices, size_t n_iter = 100)
{
    std::string device_string = devices[0].id;

    for (int k = 1; k < devices.size(); ++k)
    {
        device_string += "," + devices[k].id;
    }
    cout << "Using devices: " << device_string << endl;
    VIDI_UINT status;
    status = vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "");
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize library" << endl;
        return -1;
    }

    // enable debug output to a message file
    vidi_debug_infos(VIDI_DEBUG_SINK_FILE, "vidi_messages.log");

    // create and initialize a buffer to be used whenever data is returned from the library
    VIDI_BUFFER buffer;
    vidi_init_buffer(&buffer);

    // open the given workspace
    status = vidi_runtime_open_workspace_from_file("workspace", workspace_path.c_str());
    if (status != VIDI_SUCCESS)
    {
        vidi_get_error_message(status, &buffer);

        clog << string("failed to open workspace '") << workspace_path << "': " << buffer.data << endl;

        vidi_deinitialize();

        return -1;
    }

    {
        // load the image from the file specified in the command line
        VIDI_IMAGE image;
        vidi_init_image(&image);
        status = vidi_load_image(image_path.c_str(), &image);
        if (status != VIDI_SUCCESS)
        {
            vidi_get_error_message(status, &buffer);

            clog << "failed to read image '" << image_path.c_str() << "': " << buffer.data << endl;

            vidi_deinitialize();

            return -1;
        }

        //warm up op
        status = vidi_runtime_create_sample("workspace", "default", "my_sample");
        if (status != VIDI_SUCCESS)
        {
            vidi_deinitialize();
            return -1;
        }

		status = vidi_runtime_sample_add_image("workspace", "default", "my_sample", &image);
		if (status != VIDI_SUCCESS)
		{
			vidi_deinitialize();
			return -1;
		}

        status = vidi_runtime_sample_process("workspace", "default", "analyze", "my_sample", "");
        if (status != VIDI_SUCCESS)
        {
            vidi_deinitialize();
            return -1;
        }

        vidi_runtime_free_sample("workspace", "default", "my_sample");

        auto start = std::chrono::system_clock::now();
        for (size_t iter = 0; iter < n_iter; ++iter)
        {
            status = vidi_runtime_create_sample("workspace", "default", "my_sample");
            if (status != VIDI_SUCCESS)
            {
                vidi_deinitialize();
                return -1;
            }

			status = vidi_runtime_sample_add_image("workspace", "default", "my_sample", &image);
			if (status != VIDI_SUCCESS)
			{
				vidi_deinitialize();
				return -1;
			}

            status = vidi_runtime_sample_process("workspace", "default", "analyze", "my_sample", "");
            if (status != VIDI_SUCCESS)
            {
                vidi_deinitialize();
                return -1;
            }

            vidi_runtime_free_sample("workspace", "default", "my_sample");
        }

        auto end = chrono::system_clock::now();
        auto elapsed = chrono::duration_cast<std::chrono::milliseconds>(end - start);
        cout << "elapsed time using multiple GPUs for a single tool : " << elapsed.count() << " ms (average: " << elapsed.count() / n_iter << " ms)" << endl;

        vidi_runtime_free_sample("workspace", "default", "my_sample");

        vidi_free_image(&image);
        vidi_deinitialize();

    }

    return 0;
}

/**
* @brief Process an image n_iter times using only one GPU per tool
*/
int single_gpu_per_tool(string image_path, string workspace_path, std::vector<Device> devices, size_t n_iter = 100)
{
    std::string device_string = devices[0].id;

    for (int k = 1; k < devices.size(); ++k)
    {
        device_string += "," + devices[k].id;
    }

    VIDI_UINT status;
    status = vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, device_string.c_str());
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize library" << endl;
        return -1;
    }

    // enable debug output to a message file
    vidi_debug_infos(VIDI_DEBUG_SINK_FILE, "vidi_messages.log");

    // create and initialize a buffer to be used whenever data is returned from the library
    VIDI_BUFFER buffer;
    vidi_init_buffer(&buffer);

    // open the given workspace
    status = vidi_runtime_open_workspace_from_file("workspace", workspace_path.c_str());
    if (status != VIDI_SUCCESS)
    {
        vidi_get_error_message(status, &buffer);

        clog << string("failed to open workspace '") << workspace_path << "': " << buffer.data << endl;

        vidi_deinitialize();

        return -1;
    }

    {
        auto start = std::chrono::system_clock::now();

        std::vector<std::thread> threads;

        for (size_t k = 0; k < devices.size(); ++k)
        {
            threads.push_back(std::thread([=]()
            {

                VIDI_IMAGE image;
                vidi_init_image(&image);
                vidi_load_image(image_path.c_str(), &image);

                string sample_id = string("sample") + devices[k].id.c_str();
                for (size_t iter = 0; iter < n_iter; iter += devices.size())
                {
                    vidi_runtime_create_sample("workspace", "default", sample_id.c_str());
					vidi_runtime_sample_add_image("workspace", "default", sample_id.c_str(), &image);
                    int status = vidi_runtime_sample_process("workspace", "default", "analyze", sample_id.c_str(), "");
                    vidi_runtime_free_sample("workspace", "default", sample_id.c_str());
                }
                vidi_free_image(&image);
            }));

        }

        for (auto & th : threads)
        {
            th.join();
        }

        auto end = chrono::system_clock::now();
        auto elapsed = chrono::duration_cast<std::chrono::milliseconds>(end - start);
        cout << "elapsed time using a single GPU for a single tool (hw concurrency : " << devices.size() << "): "
            << elapsed.count() << " ms (average: " << elapsed.count() / n_iter << " ms)" << endl;

        vidi_deinitialize();

    }

    return 0;
}


/**
 * @brief run a workspace with three different GPU configurations. The first one is
 * with only one GPU. This is to get a baseline speed reading. The second one is with
 * multiple GPUs per tool which only applies to the red tool and will minimize latency.
 * The third and final one is with only one GPU per tool, which maximizes throughput.
 */
int main(int argc, char* argv[])
{
    clog
        << "Example demonstrating using multiple GPUs with ViDi's runtime library\n"
        << endl;

    VIDI_UINT status;
    status = vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "");
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize library" << endl;
        return -1;
    }

    VIDI_BUFFER  buffer;

    status = vidi_init_buffer(&buffer);
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to initialize buffer" << endl;
        vidi_deinitialize();
        return -1;
    }

    status = vidi_version(&buffer);
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to get version" << endl;
        vidi_deinitialize();
        return -1;
    }
    cout << buffer.data << endl;

    status = vidi_list_compute_devices(&buffer);
    if (status != VIDI_SUCCESS)
    {
        clog << "failed to list compute devices" << endl;
        vidi_deinitialize();
        return -1;
    }

    rapidxml::xml_document<> doc;
    doc.parse<0>(buffer.data);

    std::vector<Device> devices;
    auto devices_xml = doc.first_node("devices");
    auto device_xml = devices_xml->first_node("device");
    while (device_xml)
    {
        devices.push_back(Device(device_xml->first_attribute("id")->value(), device_xml->first_attribute("index")->value()));
        device_xml = device_xml->next_sibling();
    }

    if (devices.empty())
    {
        cerr << "no GPU available, exiting...";
        return -1;
    }
    vidi_deinitialize();

    string image_path = "..\\resources\\images\\000000.png";
    string workspace_path = "..\\resources\\runtime\\textile.vrws";

    single_gpu(image_path, workspace_path, devices[0], 50);
    multi_gpu_per_tool(image_path, workspace_path, devices, 50);
    single_gpu_per_tool(image_path, workspace_path, devices, 50);

    clog << "press enter to exit";
    while (cin.get() != '\n');

    return 0;
}
