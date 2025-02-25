#include <wtypes.h>

typedef struct Int32Color
{
	BYTE Blue;

	BYTE Green;

	BYTE Red;

	BYTE Alpha;
} Int32Color;

#define COLOR_FROM_X 1
#define COLOR_FROM_Y 2
#define ALPHA_BLENDING 4

typedef struct ColorPalette
{
	Int32Color* Colors;

	INT32 Count;

	INT32 Flags;
} ColorPalette;

typedef struct RenderInfo
{
	INT32 BytesPerPixel;

	INT32 Width;

	INT32 Height;

	INT32 Stride;

	BYTE* Buffer;

	ColorPalette* Palette;
} RenderInfo;

typedef struct Int32Rect
{
	INT32 X;

	INT32 Y;

	INT32 Width;

	INT32 Height;
} Int32Rect;

typedef struct Int32Point
{
	INT32 X;

	INT32 Y;
} Int32Point;

typedef struct Int32Pixel
{
	INT32 X;

	INT32 Y;

	INT32 Color;
} Int32Pixel;

ColorPalette* WINAPI create_palette(Int32Color* colors, INT32 count, INT32 flags);

BOOL WINAPI destroy_palette(ColorPalette** palette);

BOOL WINAPI draw_rectangles(RenderInfo* info, Int32Rect* rectangles, INT32 count);

BOOL WINAPI draw_rectangle(RenderInfo* info, INT32 x, INT32 y, INT32 width, INT32 height);

BOOL WINAPI draw_lines(RenderInfo* info, Int32Point* points, INT32 dimentions, INT32 count);

BOOL WINAPI draw_line(RenderInfo* info, INT32 x1, INT32 y1, INT32 x2, INT32 y2);

BOOL WINAPI  draw_pixels(RenderInfo* info, Int32Pixel* points, INT32 count);

BOOL WINAPI  draw_pixel(RenderInfo* info, INT32 color, INT32 x, INT32 y);

BOOL WINAPI shift_left(RenderInfo* info, INT32 count);

BOOL WINAPI clear(RenderInfo* info, Int32Color* color);