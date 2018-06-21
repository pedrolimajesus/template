using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AppComponents.Extensions;

namespace AppComponents.Data
{
    public class WebsiteToImage
    {
        private Bitmap m_Bitmap;
        private string m_Url;
        private string m_FileName = string.Empty;

        public WebsiteToImage(string url)
        {
            // Without file 
            m_Url = url;
        }

        public WebsiteToImage(string url, string fileName)
        {
            // With file 
            m_Url = url;
            m_FileName = fileName;
        }

        public Bitmap Generate()
        {
            // Thread 
            var m_thread = new Thread(_Generate);
            m_thread.SetApartmentState(ApartmentState.STA);
            m_thread.Start();
            m_thread.Join();
            return m_Bitmap;
        }

        private void _Generate()
        {
            var browser = new WebBrowser { ScrollBarsEnabled = false };
            browser.Navigate(m_Url);
            browser.DocumentCompleted += WebBrowser_DocumentCompleted;

            while (browser.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
            }

            browser.Dispose();
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // Capture 
            var browser = (WebBrowser)sender;
            browser.ClientSize = new Size(browser.Document.Body.ScrollRectangle.Width, browser.Document.Body.ScrollRectangle.Bottom);
            browser.ScrollBarsEnabled = false;
            m_Bitmap = new Bitmap(browser.Document.Body.ScrollRectangle.Width, browser.Document.Body.ScrollRectangle.Bottom);
            browser.BringToFront();
            browser.DrawToBitmap(m_Bitmap, browser.Bounds);

            // Save as file? 
            if (m_FileName.Length > 0)
            {
                // Save 
                m_Bitmap.SaveJPG100(m_FileName);
            }
        }
    }

}
