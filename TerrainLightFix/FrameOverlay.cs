using Sims3.SimIFace;
using Sims3.UI;

namespace Arro.tlmf
{
    public class FrameOverlay
    {
        private static Layout sLayout;
        private static WindowBase sOverlayWindow;

        public static void Show()
        {
            if (sOverlayWindow == null)
            {
                ResourceKey resKey = ResourceKey.CreateUILayoutKey("ScreenCaptureOverlay", 0U);
                sLayout = UIManager.LoadLayoutAndAddToWindow(resKey, UICategory.NewHUD);
                sOverlayWindow = sLayout.GetWindowByExportID(1);
                sOverlayWindow.MoveToBack();
            }

            UIImage image = ScreenGrabNormal();
            var imageDrawable = (sOverlayWindow as Sims3.UI.GameEntry.ScreenCaptureOverlay)?.Drawable as ImageDrawable;
            if (imageDrawable != null && image != null)
            {
                imageDrawable.Image = image;
                sOverlayWindow.Invalidate();
            }

            sOverlayWindow.Visible = true;
        }

        public static void Hide()
        {
            if (sOverlayWindow != null)
            {
                sOverlayWindow.Visible = false;
                sOverlayWindow = null;
            }
            if (sLayout != null)
            {
                sLayout.Shutdown();
                sLayout.Dispose();
                sLayout = null;
            }
        }

        public static UIImage ScreenGrabNormal()
        {
            WindowBase sceneWindow = UIManager.GetSceneWindow();
            if (sceneWindow != null)
            {
                Vector2 vector = sceneWindow.WindowToScreen(Vector2.Zero);
                uint x = (uint)vector.x;
                uint y = (uint)vector.y;
                uint width = (uint)sceneWindow.Area.Width;
                uint height = (uint)sceneWindow.Area.Height;
                return UIManager.CaptureSceneAsImage(PaintingStyle.Normal, x, y, width, height, 0U);
            }
            return null;
        }
    }
}