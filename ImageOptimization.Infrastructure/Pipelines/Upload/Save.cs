using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Pipelines.Upload;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Sitecore.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageOptimization.Infrastructure.Pipelines.Upload
{
    public class Save : UploadProcessor
    {
        /// <summary>
        /// Runs the processor.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <exception cref="T:System.Exception"><c>Exception</c>.</exception>
        public void Process(UploadArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            for (int i = 0; i < args.Files.Count; i++)
            {
                HttpPostedFile httpPostedFile = args.Files[i];
                if (!string.IsNullOrEmpty(httpPostedFile.FileName))
                {
                    try
                    {
                        bool flag = UploadProcessor.IsUnpack(args, httpPostedFile);
                        if (args.FileOnly)
                        {
                            if (flag)
                            {
                                Save.UnpackToFile(args, httpPostedFile);
                            }
                            else
                            {
                                string filename = this.UploadToFile(args, httpPostedFile);
                                if (i == 0)
                                {
                                    args.Properties["filename"] = FileHandle.GetFileHandle(filename);
                                }
                            }
                        }
                        else
                        {
                            // Attempt to Compress File
                            byte[] fileData = new byte[httpPostedFile.ContentLength];
                            var service = new Integrations.TinyPng.Service();

                            httpPostedFile.InputStream.Read(fileData, 0, httpPostedFile.ContentLength);

                            byte[] result = service.OptimizeImage(fileData);

                            List<MediaUploadResult> list;
                            using (new SecurityDisabler())
                            {
                                list = UploadMedia(result, args.Folder, args.Versioned, args.Language, args.GetFileParameter(httpPostedFile.FileName, "alt"),
                                    args.Overwrite, (args.Destination == UploadDestination.File));
                            }
                            Log.Audit(this, "Upload: {0}", new string[]
                            {
                                httpPostedFile.FileName
                            });
                            foreach (MediaUploadResult current in list)
                            {
                                this.ProcessItem(args, current.Item, current.Path);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Could not save posted file: " + httpPostedFile.FileName, exception, this);
                        throw;
                    }
                }
            }
        }

        private List<MediaUploadResult> UploadMedia(byte[] fileData, string fileName, bool flag, string folder, bool versioned, Language language, string altText, bool overwrite, bool fileBased)
        {
            List<MediaUploadResult> list = new List<MediaUploadResult>();

            bool itemFlag = string.Compare(Path.GetExtension(fileName), ".zip", StringComparison.InvariantCultureIgnoreCase) == 0;

            if (itemFlag)
            {
                // Unpack to Database
                //string text = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));
                
            }
            else
            {
                // Upload to Database
                MediaUploadResult mediaUploadResult = new MediaUploadResult();
                list.Add(mediaUploadResult);

                mediaUploadResult.Path = FileUtil.MakePath(folder, Path.GetFileName(fileName), '/');

            }
        }

        /// <summary>
        /// Processes the item.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="mediaItem">The media item.</param>
        /// <param name="path">The path.</param>
        private void ProcessItem(UploadArgs args, MediaItem mediaItem, string path)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(mediaItem, "mediaItem");
            Assert.ArgumentNotNull(path, "path");
            if (args.Destination == UploadDestination.Database)
            {
                Log.Info("Media Item has been uploaded to database: " + path, this);
            }
            else
            {
                Log.Info("Media Item has been uploaded to file system: " + path, this);
            }
            args.UploadedItems.Add(mediaItem.InnerItem);
        }

        /// <summary>
        /// Unpacks to file.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        private static void UnpackToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(file, "file");
            string filename = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));
            file.SaveAs(filename);
            using (ZipReader zipReader = new ZipReader(filename))
            {
                foreach (ZipEntry current in zipReader.Entries)
                {
                    string text = FileUtil.MakePath(args.Folder, current.Name, '\\');
                    if (current.IsDirectory)
                    {
                        Directory.CreateDirectory(text);
                    }
                    else
                    {
                        if (!args.Overwrite)
                        {
                            text = FileUtil.GetUniqueFilename(text);
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(text));
                        lock (FileUtil.GetFileLock(text))
                        {
                            FileUtil.CreateFile(text, current.GetStream(), true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Uploads to file.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        /// <returns>The name of the uploaded file</returns>
        private string UploadToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(file, "file");
            string fileName = FileUtil.MakePath(args.Folder, Path.GetFileName(file.FileName), '\\');
            if (!args.Overwrite)
            {
                fileName = FileUtil.GetUniqueFilename(fileName);
            }

            // Automatically Run Images Against TinyPng
            if (IsImage(file))
            {
                byte[] fileData = null;
                using (var binaryReader = new BinaryReader(file.InputStream))
                {
                    fileData = binaryReader.ReadBytes(file.ContentLength);
                }

                if (fileData != null)
                {
                    var service = new Integrations.TinyPng.Service();

                    var imgBytes = service.OptimizeImage(fileData);

                    using (var ms = new System.IO.MemoryStream(imgBytes))
                    {
                        using (var img = System.Drawing.Image.FromStream(ms))
                        {
                            img.Save(fileName);
                        }
                    }
                }
            } else
            {
                file.SaveAs(fileName);
            }

            Log.Info("File has been uploaded: " + fileName, this);
            return Assert.ResultNotNull<string>(fileName);
        }

        public static bool IsImage(HttpPostedFile postedFile)
        {
            if (postedFile.ContentType.ToLower() != "image/jpg" &&
                        postedFile.ContentType.ToLower() != "image/jpeg" &&
                        postedFile.ContentType.ToLower() != "image/pjpeg" &&
                        postedFile.ContentType.ToLower() != "image/gif" &&
                        postedFile.ContentType.ToLower() != "image/x-png" &&
                        postedFile.ContentType.ToLower() != "image/png")
            {
                return false;
            }
            
            if (Path.GetExtension(postedFile.FileName).ToLower() != ".jpg"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".png"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".gif"
                && Path.GetExtension(postedFile.FileName).ToLower() != ".jpeg")
            {
                return false;
            }

            return true;
        }
    }
}
