#ifdef _DEBUG
#include <stdio.h>
#endif

#include "bass_wasapi_handler.h"
#include "bass/basswasapi.h"

BOOL is_initialized = FALSE;
DWORD channel_handle = 0;

//I have no idea how to prevent linking against this routine in msvcrt.
//It doesn't exist on Windows XP.
//Hopefully it doesn't do anything important.
int _except_handler4_common() {
	return 0;
}

BOOL BASSWASAPIHANDLERDEF(BASS_WASAPI_HANDLER_Init)(int device, DWORD freq, DWORD chans, DWORD flags, float buffer, float period, void *user) {
	BOOL success;
	if (is_initialized) {
		return FALSE;
	}
	channel_handle = 0;
	is_initialized = TRUE;
	success = BASS_WASAPI_Init(device, freq, chans, flags, buffer, period, &BASS_WASAPI_HANDLER_StreamProc, user);
	if (!success) {
#if _DEBUG
		printf("BASS WASAPI HANDLER enabled.\n");
#endif
	}
	return success;
}

BOOL BASSWASAPIHANDLERDEF(BASS_WASAPI_HANDLER_Free)() {
	if (!is_initialized) {
		return FALSE;
	}
	channel_handle = 0;
	is_initialized = FALSE;
#if _DEBUG
	printf("BASS WASAPI HANDLER released.\n");
#endif
	return TRUE;
}

BOOL BASSWASAPIHANDLERDEF(BASS_WASAPI_HANDLER_StreamGet)(DWORD* handle) {
	if (!channel_handle) {
		return FALSE;
	}
	*handle = channel_handle;
#if _DEBUG
	printf("BASS WASAPI HANDLER stream: %d.\n", channel_handle);
#endif
	return TRUE;
}

BOOL BASSWASAPIHANDLERDEF(BASS_WASAPI_HANDLER_StreamSet)(DWORD handle) {
	channel_handle = handle;
#if _DEBUG
	printf("BASS WASAPI HANDLER stream: %d.\n", channel_handle);
#endif
	return TRUE;
}

DWORD CALLBACK BASS_WASAPI_HANDLER_StreamProc(void *buffer, DWORD length, void *user) {
	DWORD result;
	if (!channel_handle) {
		result = 0;
	}
	else {
		result = BASS_ChannelGetData(channel_handle, buffer, length);
		switch (result)
		{
		case BASS_STREAMPROC_END:
		case BASS_ERROR_UNKNOWN:
			result = 0;
			break;
		default:
#if _DEBUG
			printf("Write %d bytes to WASAPI buffer\n", result);
#endif
			break;
		}
	}
	if (result < length) {
#if _DEBUG
		printf("Buffer under run while writing to WASAPI buffer.\n");
#endif
	}
	return result;
}