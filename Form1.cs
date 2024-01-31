using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;

namespace Facebook_scraper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            webBrowser1.Url = new Uri(textBox1.Text);
            webBrowser1.Refresh();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            String s = webBrowser1.Document.Body.InnerHtml;
            Dictionary<string, string> altAttributes = ExtractAltAttributes(s);

            Console.WriteLine("Attributi 'alt' estratti:");
            foreach (var altAttribute in altAttributes)
            {
                Console.WriteLine(altAttribute.Value);
            }

            List < Tuple<string, string> > imagesDictionary = await GetImagesFromWebBrowserAsync(webBrowser1);
            await DisplayImagesAndAltTextAsync(panel1, imagesDictionary, textBox2.Text);

            panel1.Refresh();
        }
        static Dictionary<string, string> ExtractAltAttributes(string html)
        {
            Dictionary<string, string> altAttributes = new Dictionary<string, string>();
            String prefix = "potrebbe essere";

            // Utilizza un'espressione regolare per trovare tutti gli attributi 'alt' nelle immagini
            Regex regex = new Regex($@"<img[^>]*alt=[""']{Regex.Escape(prefix)}([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(html);

            foreach (Match match in matches)
            {
                string altAttribute = "alt=\"";
                string urlAttribute = "src=\"";
                int startIndex = match.Value.IndexOf(altAttribute);
                int startIndexu = match.Value.IndexOf(urlAttribute);

                if (startIndex != -1 && startIndexu != -1)
                {
                    startIndex += altAttribute.Length;
                    startIndexu += urlAttribute.Length;
                    int endIndex = match.Value.IndexOf("\"", startIndex);
                    int endIndexu = match.Value.IndexOf("\"", startIndexu);

                    if (endIndex != -1 && endIndexu != -1)
                    {
                        altAttributes.Add(match.Value.Substring(startIndexu, endIndexu - startIndexu), match.Value.Substring(startIndex, endIndex - startIndex));
                    }
                }
            }

            return altAttributes;
        }
        private async Task<List<Tuple<string, string>>> GetImagesFromWebBrowserAsync(WebBrowser webBrowser)
        {

            List<Tuple<string, string>> imagesList = new List<Tuple<string, string>>();

            // Esegui uno script JavaScript per ottenere informazioni sulle immagini
            var script = "var images = [];"
                       + "var imgs = document.getElementsByTagName('img');"
                       + "for (var i = 0; i < imgs.length; i++) {"
                       + "  var img = imgs[i];"
                       + "  var src = img.src;"
                       + "  var alt = img.alt;"
                       + "  images.push([src, alt]);"
                       + "}"
                       + "JSON.stringify(images);";

            string result = webBrowser.Document.InvokeScript("eval", new object[] { script }) as string;

            if (!string.IsNullOrEmpty(result))
            {
                // Deserializza il risultato JSON in una lista di tuple
                imagesList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string[]>>(result)
                    .Select(x => Tuple.Create(x[0], x[1]))
                    .ToList();
            }

            return imagesList;
        }

        int topMargin = 10;
        int verticalSpacing = 10;
        private async Task DisplayImagesAndAltTextAsync(Panel panel, List<Tuple<string, string>> imagesDictionary, string filter)
        {
            if (checkbox1.Checked)
            {
                panel.Controls.Clear();
                topMargin = 10;
            }

            foreach (var entry in imagesDictionary)
            {
                if (entry.Item2.ToLower().Contains(filter))
                {
                    Image image = await DownloadImageAsync(entry.Item1);

                    // Crea un PictureBox per l'immagine
                    PictureBox pictureBox = new PictureBox();
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox.Image = image;
                    pictureBox.Size = new Size(200, 150);
                    pictureBox.Top = topMargin;

                    // Crea un Label per il testo alt
                    Label altLabel = new Label();
                    altLabel.Text = entry.Item2;
                    altLabel.AutoSize = true;
                    altLabel.Top = topMargin + pictureBox.Height + verticalSpacing;

                    // Aggiungi PictureBox e Label al panel
                    panel.Controls.Add(pictureBox);
                    panel.Controls.Add(altLabel);

                    // Aggiorna la posizione per il prossimo set di immagine e testo alt
                    topMargin += pictureBox.Height + altLabel.Height + verticalSpacing * 2;
                }
            }
        }

        private async Task<Image> DownloadImageAsync(string imageUrl)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    byte[] data = await client.DownloadDataTaskAsync(new Uri(imageUrl));
                    using (var ms = new System.IO.MemoryStream(data))
                    {
                        return Image.FromStream(ms);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore nel scaricare l'immagine: {ex.Message}");
                    return null;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1_TextChanged(null, null);
        }
    }
}
