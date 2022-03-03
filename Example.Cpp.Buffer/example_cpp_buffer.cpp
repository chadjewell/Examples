/**
 * @file example_cpp_buffer.cpp
 * @brief Example demonstrating how to responsibly manage a VIDI_BUFFER.
 * No error management is done is this example
 */

#include "vidi.h"
#include <iostream>

using namespace std;

/**
 * @brief initialize the buffer and use it to get version data
 * and license information
 */
int main()
{
    VIDI_UINT status = VIDI_SUCCESS;

    // send debug info to the console
    status = vidi_debug_infos(VIDI_DEBUG_SINK_CONSOLE, "");
    if (status != VIDI_SUCCESS)
    {
        cerr << "failed to enable debug infos";
        return -1;
    }

    // initialize ViDi library
    status = vidi_initialize(VIDI_GPU_MODE_NO_SUPPORT, "");
    if (status != VIDI_SUCCESS)
    {
        cerr << "failed to initialize vidi";
        return -1;
    }

    VIDI_BUFFER buffer;
    status = vidi_init_buffer(&buffer);
    if (status != VIDI_SUCCESS)
    {
        cerr << "failed to initialize vidi buffer";
        return -1;
    }

    {   // retrieve version of the Dll
        status = vidi_version(&buffer);
        if (status != VIDI_SUCCESS)
        {
            cerr << "failed to get version";
            vidi_deinitialize();
            return -1;
        }
        cout << buffer.data << endl;
    }

    {   // retrieve license information
        status = vidi_license_get_info(&buffer);
        if (status != VIDI_SUCCESS)
        {
            cerr << "failed to get license information";
            vidi_deinitialize();
            return -1;
        }
        cout << buffer.data << endl;
    }

    // vidi_deinitialize will free the buffer
    // otherwise, call vidi_free_buffer()
    vidi_deinitialize();
    return 0;
}
