/**
 * @file example_cpp_error_handling.cpp
 * @brief Example demonstrating how to get and display an error message.
 */

#include "vidi.h"
#include <iostream>
#include <string>

// RapidXML is a lightweight, header only, fast xml parser.
#define USE_RAPIDXML
#ifdef USE_RAPIDXML
#include "../include/rapidxml/rapidxml.hpp"
#endif

// customer error return code (usually -1)
int error_return_code = 0;

/**
 * @brief gets the error message from the status using VIDI_BUFFER
 *
 * @return a string containing the last error message or a string indicating that we couldn't get the last error message
 */
std::string get_last_error_message(VIDI_UINT status)
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
        return error_return_code;                \
    }                                            \
}

/**
 * @brief initialize vidi twice and use the the CHECK_STATUS macro to display the error message
 *
 * The error message "ViDi is already initialized" will be printed by the CHECK_STATUS macro
 */
int main()
{
    VIDI_UINT status;
    status = vidi_initialize(VIDI_GPU_MODE_NO_SUPPORT, "");
    if (status != VIDI_SUCCESS)
    {
        std::cerr << "failed to initialize vidi";
        return -1;
    }

    status = vidi_initialize(VIDI_GPU_MODE_NO_SUPPORT, "");
    CHECK_STATUS(status);

    vidi_deinitialize();

    return 0;
}
