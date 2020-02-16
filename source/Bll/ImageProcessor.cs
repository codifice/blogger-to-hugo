using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BloggerToHugo.Model;
using Newtonsoft.Json;

namespace BloggerToHugo.Bll
{
    public class ImageProcessor
    {
        public ImageProcessor()
        {
            DownloadedImageDict = new Dictionary<string, string>();
        }

        public Dictionary<string, string> DownloadedImageDict { get; set; }

        public void PrepareImageDict(string imageDirctory)
        {
            var allImageFiles = Directory.GetFiles(imageDirctory)
                .Where(x => x.EndsWith(".json") == false).ToList();

            var metaFiles = Directory.GetFiles(imageDirctory, "*.json");

            foreach (var file in metaFiles)
            {
                var model = JsonConvert.DeserializeObject<ImageMeta>(File.ReadAllText(file));

                if (string.IsNullOrEmpty(model.url) == false)
                {
                    var localFile =
                        allImageFiles.FirstOrDefault(x => x.Contains(Path.GetFileNameWithoutExtension(file)));
                    DownloadedImageDict.Add(model.url, localFile);
                }
            }
        }

        public void ProcessImage(BlogPostModel model, string postPath)
        {
            DownloadImages(model, postPath);

            ReplaceImageInContent(model);
        }

        private void ReplaceImageInContent(BlogPostModel model)
        {
            foreach (var item in model.Images)
            {
                model.Content = model.Content.Replace(item.OriginalUrl, item.LocalPath);
            }
        }

        public void DownloadImages(BlogPostModel model, string postPath)
        {
            if (model.Images.Count > 0)
            {
                var path = Path.Combine(postPath, "images");

                Directory.CreateDirectory(path);
                string newFilePath;
                foreach (var item in model.Images)
                {
                    string filename = null;
                    using (var wc = new WebClient())
                    {
                        var imageData = wc.DownloadData(item.OriginalUrl);
                        var responseHeaders = wc.ResponseHeaders;
                        var disposition = responseHeaders["Content-Disposition"];
                        var idx = disposition.IndexOf("filename=", StringComparison.OrdinalIgnoreCase);
                        if (idx > -1)
                        {
                            filename = disposition.Substring(idx + "filename=".Length)
                                .Split(new[]{'"'}, StringSplitOptions.RemoveEmptyEntries).First();
                        }
                        else
                        {
                            filename = Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".png";
                        }

                        newFilePath = Path.Combine(path, Path.GetFileName(filename));
                        using (var fi = File.OpenWrite(newFilePath))
                        {
                            fi.Write(imageData, 0, imageData.Length);
                        }
                    }

                    item.SaveToPath = newFilePath;
                    item.LocalPath = Path.Combine("images", Path.GetFileName(newFilePath)).Replace("\\","/");
                }
            }
        }

        internal void PrepareImageDict(object offLineImagePath)
        {
            throw new NotImplementedException();
        }
    }
}