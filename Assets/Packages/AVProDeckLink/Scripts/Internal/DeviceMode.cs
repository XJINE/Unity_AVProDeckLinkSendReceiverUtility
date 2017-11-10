using System.Text;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    public class DeviceMode
    {
        private Device _device;
        private int _modeIndex;
        private int _width, _height;
        private float _frameRate;
        private long _frameDuration;
        private DeckLinkPlugin.PixelFormat _pixelFormat;
        private string _modeDesc;
        private string _pixelFormatDesc;
        private int _pitch;
        private FieldMode _fieldMode;
        private string _fieldModeString;

        public enum FieldMode
        {
            Progressive,
            Progressive_Segmented,
            Interlaced_UpperFirst,
            Interlaced_LowerFirst,
        }

        public Device Device
        {
            get { return _device; }
        }

        public int Index
        {
            get { return _modeIndex; }
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public float FrameRate
        {
            get { return _frameRate; }
        }

        public long FrameDuration
        {
            get { return _frameDuration; }
        }

        public DeckLinkPlugin.PixelFormat PixelFormat
        {
            get { return _pixelFormat; }
        }

        public string ModeDescription
        {
            get { return _modeDesc; }
        }

        public string PixelFormatDescription
        {
            get { return _pixelFormatDesc; }
        }

        public int Pitch
        {
            get { return _pitch; }
        }

        public bool InterlacedFieldMode
        {
            get { return _fieldMode != FieldMode.Progressive; }
        }

        public string FieldModeString
        {
            get { return _fieldModeString; }
        }

        public DeviceMode(Device device, int modeIndex, int width, int height, float frameRate, long frameDuration, FieldMode fieldMode, string modeDesc, string pixelFormatDesc)
        {
            _device = device;
            _modeIndex = modeIndex;
            _width = width;
            _height = height;
            _frameRate = frameRate;
            _fieldMode = fieldMode;
            _frameDuration = frameDuration;
            _modeDesc = modeDesc;
            _pixelFormatDesc = pixelFormatDesc;
            _pixelFormat = DeckLinkPlugin.GetPixelFormat(_pixelFormatDesc);
            _pitch = GetPitch(_width, _pixelFormat);

            _fieldModeString = "p";
            switch (_fieldMode)
            {
                case FieldMode.Interlaced_UpperFirst:
                case FieldMode.Interlaced_LowerFirst:
                    _fieldModeString = "i";
                    break;
                case FieldMode.Progressive_Segmented:
                    _fieldModeString = "PsF";
                    break;
            }
        }

        public static int GetPitch(int width, DeckLinkPlugin.PixelFormat pixelFormat)
        {
            int result = 0;
            switch (pixelFormat)
            {
                case DeckLinkPlugin.PixelFormat.YCbCr_8bpp_422:
                    result = (width * 16) / 8;
                    break;
                case DeckLinkPlugin.PixelFormat.YCbCr_10bpp_422:
                    result = (((width + 47) / 48) * 128);
                    break;
                case DeckLinkPlugin.PixelFormat.ARGB_8bpp_444:
                    result = (width * 32) / 8;
                    break;
                case DeckLinkPlugin.PixelFormat.BGRA_8bpp_444:
                    result = (width * 32) / 8;
                    break;
                case DeckLinkPlugin.PixelFormat.RGB_10bpp_444:
                case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444:
                case DeckLinkPlugin.PixelFormat.RGBX_10bpp_444_LE:
                    result = ((width + 63) / 64) * 256;
                    break;
            }
            return result;
        }
    }
}