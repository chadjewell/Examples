/**
 * @file example_cpp_image.cpp
 * @brief Example demonstrating common usecases of VIDI_IMAGE
 * No error management is done is this example
 */

#include "vidi.h"
#include <iostream>
#include <fstream>
#include <vector>

/**
 * @brief demonstrate three typical use cases for using and manipulating VIDI_IMAGE
 *
 * We take a look at creating both a user managed and ViDi managed VIDI_IMAGE and
 * demonstrate how to initialize, save, and load a VIDI_IMAGE
 */
int main()
{
    // send debug info to the console
    if (vidi_debug_infos(VIDI_DEBUG_SINK_CONSOLE, "") != VIDI_SUCCESS)
    {
        std::cerr << "failed to enable debug infos";
        return -1;
    }

    if (vidi_initialize(VIDI_GPU_MODE_NO_SUPPORT, "") != VIDI_SUCCESS)
    {
        std::cerr << "failed to initialize vidi";
        return -1;
    }

    // Typical use cases : image from camera, 3rd party library. User has the image as an char array and wants ViDi to use it.
    {
        VIDI_UINT status;
        VIDI_IMAGE img; // Memory Not managed by ViDi
        img.channels = 1;
        img.channel_depth = VIDI_IMG_8U;
        img.height = 255;
        img.width = 255;
        img.step = img.width;

        // Allocate img data
        img.data = new uint8_t[img.height * img.width * img.channels];

        // Fill it with 42
        for (size_t k = 0; k < img.height * img.step; ++k)
        {
            // here you would fill data with your desired image data
            ((uint8_t*)img.data)[k] = 42;
        }
        // save images
        status = vidi_save_image("img42.png", &img);
        if (status != VIDI_SUCCESS)
        {
            std::cerr << "failed to save image";
            vidi_deinitialize();
            return -1;
        }

        // Deallocates image
        delete[]((uint8_t*)img.data);
    }

    // Typical use case : load an image from the filesystem, the user wants vidi to use a file that is on the filesystem.
    {
        VIDI_UINT status;
        VIDI_IMAGE img;
        vidi_init_image(&img); //Managed by ViDi
        status = vidi_load_image("img42.png", &img);
        if (status != VIDI_SUCCESS)
        {
            std::cerr << "failed to load image";
            vidi_deinitialize();
            return -1;
        }

        std::cout << "number of channels : " << img.channels << std::endl
            << "channel depth : " << img.channel_depth << std::endl
            << "img height : " << img.height << std::endl
            << "img width : " << img.width << std::endl
            << "img step : " << img.step << std::endl
            << "first pixel value : " << ((img.channel_depth == VIDI_IMG_8U) ? ((uint8_t*)img.data)[0] : ((uint16_t*)img.data)[0]) << std::endl;

        status = vidi_save_image("img_copy.png", &img);
        if (status != VIDI_SUCCESS)
        {
            std::cerr << "failed to save image";
            vidi_deinitialize();
            return -1;
        }
    }


    // Typical use case : get an image encoded as PNG/BMP/TIFF through a pipe/web request. Using this method, ViDi can directly load this image from the memory.
    {
        VIDI_UINT status;
        VIDI_IMAGE img;
        VIDI_BUFFER buffer;    // Buffer not managed by ViDi
        vidi_init_image(&img); // Image Managed by ViDi

        // load the image file and store it in buffer
        std::ifstream img_file("img42.png", std::ios::binary | std::ios::in);
        img_file.seekg(0, img_file.end);
        size_t size = img_file.tellg();
        img_file.seekg(0, img_file.beg);
        std::vector<char> imgdata(size);
        if (img_file.read(imgdata.data(), size))
        {
            buffer.data = &imgdata.front();
            buffer.size = imgdata.size();
            // load it with vidi
            status = vidi_load_image_from_memory(&buffer, VIDI_IMAGE_FORMAT_PNG, &img);
            if (status != VIDI_SUCCESS)
            {
                std::cerr << "failed to load image";
                vidi_deinitialize();
                return -1;
            }

            std::cout << "number of channels : " << img.channels << std::endl
                << "channel depth : " << img.channel_depth << std::endl
                << "img height : " << img.height << std::endl
                << "img width : " << img.width << std::endl
                << "img step : " << img.step << std::endl
                << "first pixel value : " << ((img.channel_depth == VIDI_IMG_8U) ? ((uint8_t*)img.data)[0] : ((uint16_t*)img.data)[0]) << std::endl;
        }
    }

    vidi_deinitialize();
    return 0;
}

