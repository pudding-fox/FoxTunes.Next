#ifdef _DEBUG
#include <stdio.h>
#endif

#include "bass_asio_handler.h"
#include "bass/bassasio.h"

BOOL is_initialized = FALSE;
DWORD channel_handle = 0;

//I have no idea how to prevent linking against this routine in msvcrt.
//It doesn't exist on Windows XP.
//Hopefully it doesn't do anything important.
int _except_handler4_common() {
	return 0;
}

BOOL BASSASIOHANDLERDEF(BASS_ASIO_HANDLER_Init)() {
	if (is_initialized) {
		return FALSE;
	}
	channel_handle = 0;
	is_initialized = TRUE;
#if _DEBUG
	printf("BASS ASIO HANDLER initialized.\n");
#endif
	return TRUE;
}

BOOL BASSASIOHANDLERDEF(BASS_ASIO_HANDLER_Free)() {
	if (!is_initialized) {
		return FALSE;
	}
	channel_handle = 0;
	is_initialized = FALSE;
#if _DEBUG
	printf("BASS ASIO HANDLER released.\n");
#endif
	return TRUE;
}

BOOL BASSASIOHANDLERDEF(BASS_ASIO_HANDLER_StreamGet)(DWORD* handle) {
	if (!channel_handle) {
		return FALSE;
	}
	*handle = channel_handle;
#if _DEBUG
	printf("BASS ASIO HANDLER stream: %d.\n", channel_handle);
#endif
	return TRUE;
}

BOOL BASSASIOHANDLERDEF(BASS_ASIO_HANDLER_StreamSet)(DWORD handle) {
	channel_handle = handle;
#if _DEBUG
	printf("BASS ASIO HANDLER stream: %d.\n", channel_handle);
#endif
	return TRUE;
}

BOOL BASSASIOHANDLERDEF(BASS_ASIO_HANDLER_ChannelEnable)(BOOL input, DWORD channel, void *user) {
	BOOL success = BASS_ASIO_ChannelEnable(input, channel, &BASS_ASIO_HANDLER_StreamProc, user);
	if (!success) {
#if _DEBUG
		printf("BASS ASIO HANDLER enabled.\n");
#endif
	}
	return success;
}

DWORD CALLBACK BASS_ASIO_HANDLER_StreamProc(BOOL input, DWORD channel, void *buffer, DWORD length, void *user) {
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
			printf("Write %d bytes to ASIO buffer\n", result);
#endif
			break;
		}
	}
	if (result < length) {
#if _DEBUG
		printf("Buffer under run while writing to ASIO buffer.\n");
#endif
	}
	return result;
}