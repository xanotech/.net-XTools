using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Xanotech.Tools {
    public static class DrawingTool {

        public static Image Read(string filename) {
            try {
                return Image.FromFile(filename);
            } catch (ArgumentException) {
                try {
                    using (var icon = new Icon(filename))
                        return icon.ToBitmap();
                } catch {
                    // Figure it out later
                } // End try-catch
            } // End try-catch
            return null;
        } // End method



        public static Image Read(string filename, int width, int height) {
            var img = Read(filename);
            if (img == null)
                return img;

            if (img.Width == width && img.Height == height)
                return img;

            using (img)
                return img.Resize(width, height);
        } // End method



        public static Image Resize(this Image img, int width, int height) {
            var newImage = new Bitmap(width, height);
            using (var g = Graphics.FromImage(newImage)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, width, height);
            } // End using
            return newImage;
        } // End method



        public static Icon ToIcon(this Image img) {
            using (var bmp = new Bitmap(img)) {
                var ptr = bmp.GetHicon();
                return Icon.FromHandle(ptr);
            } // End using
        } // End method

    } // End class
} // End namespace