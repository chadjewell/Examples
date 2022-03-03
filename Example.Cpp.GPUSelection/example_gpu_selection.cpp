/** @file example_gpu_selection.cpp
*	
*	Simple example on how to get encapsulate the buffer management inside a class.
*	No error management is done is this example
*/

#include "vidi.h"
#include <iostream>

using namespace std;

 void print_gpu_devices()
 {
	 VIDI_BUFFER buffer;
	 vidi_init_buffer(&buffer);
	 vidi_optimized_gpu_memory(0);
	 vidi_list_compute_devices(&buffer);
	 cout << buffer.data << endl << endl;
	 vidi_free_buffer(&buffer);
 }

int main()
{
	/*
	* Debug infos to console
	*/
	if (vidi_debug_infos(VIDI_DEBUG_SINK_CONSOLE, "") != VIDI_SUCCESS)
	{
		cerr << "failed to enable debug infos";
		return -1;
	}

	/*
	*  Initialize VIDI using a single devices per tool
	*/
	if (vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "") == VIDI_SUCCESS)
	{
		{

		}
		vidi_deinitialize();
	}

	/*
	*  Initialize VIDI using a single devices per tool
	*  Uses Optimized GPU memory (1 GB)
	*/
	if (vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "") == VIDI_SUCCESS)
	{
		print_gpu_devices();
		vidi_deinitialize();
	}

	/*
	*  Initialize VIDI using a single devices per tool
	*  Uses Optimized GPU memory (Automatic)
	*/
	if (vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "") == VIDI_SUCCESS)
	{
		print_gpu_devices();
		vidi_deinitialize();
	}

	/*
	*	Initialize VIDI using a single devices per tool
	*	And uses GPU 1
	*	Will fail if no gup is present
	*/
	if (vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "1") == VIDI_SUCCESS)
	{
		print_gpu_devices();
		vidi_deinitialize();
	}
	else
		cerr << "cannot initialize vidi with provided gpu list" << endl << endl;

	/*
	*	Initialize VIDI using a single devices per tool
	*	And uses GPU 0 and 1
	*	Will fail if less than 2 gpus are present
	*/
	if (vidi_initialize(VIDI_GPU_SINGLE_DEVICE_PER_TOOL, "0,1") == VIDI_SUCCESS)
	{
		print_gpu_devices();
		vidi_deinitialize();
	}
	else
		cerr << "cannot initialize vidi with provided gpu list" << endl << endl;

	/*
	*  Initialize VIDI using mutliple devices per tool
	*  Do not use optimized GPU Memory
	*/
	if (vidi_initialize(VIDI_GPU_MULTIPLE_DEVICES_PER_TOOL, "") == VIDI_SUCCESS)
	{
		print_gpu_devices();
		vidi_deinitialize();
	}

	/*
	*  Initialize VIDI with no GPU support
	*/
	if (vidi_initialize(VIDI_GPU_MODE_NO_SUPPORT, "") == VIDI_SUCCESS)
	{
		print_gpu_devices();
		vidi_deinitialize();
	}
	
	return 0;
}
