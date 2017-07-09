using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Pipelines.GetMediaCreatorOptions;
using Sitecore.Pipelines.Upload;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Sitecore.Zip;
using System;
using System.IO;
using System.Web;

namespace Sitecore.Foundation.ImageCompression.Pipelines.Upload
{
    public class Save : UploadProcessor
    {
        private readonly ImageCompressionManager _compressionManager;

        public Save()
        {
            _compressionManager = new ImageCompressionManager();
        }

        public void Process(UploadArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");

            for (int index = 0; index < args.Files.Count; ++index)
            {
                HttpPostedFile file = args.Files[index];
                if (!string.IsNullOrEmpty(file.FileName))
                {
                    try
                    {
                        bool flag = UploadProcessor.IsUnpack(args, file);
                        if (args.FileOnly)
                        {
                            if (flag)
                            {
                                Save.UnpackToFile(args, file);
                            }
                            else
                            {
                                string uploadFile = this.UploadToFile(args, file);
                                if (index == 0)
                                    args.Properties["filename"] = (object)FileHandle.GetFileHandle(uploadFile);
                            }
                        }
                        else
                        {
                            byte[] fileData = null;
                            file.InputStream.Position = 0;
                            using (var binaryReader = new BinaryReader(file.InputStream))
                            {
                                fileData = binaryReader.ReadBytes(file.ContentLength);
                            }

                            if (fileData == null)
                            {
                                Log.Error($"Could not process the saved posted file {file.FileName}", null, (object)this);
                                return;
                            }

                            byte[] compressedBytes = _compressionManager.OptimizeImage(fileData);

                            //List<Sitecore.Resources.Media.MediaUploadResult> mediaUploadResultList;

                            using (new SecurityDisabler())
                                Upload(file, compressedBytes, flag, args, (args.Destination == UploadDestination.File));

                            Log.Audit((object)this, "Upload: {0}", new string[1]
                            {
                                file.FileName
                            });

                            //foreach (Sitecore.Resources.Media.MediaUploadResult mediaUploadResult in mediaUploadResultList)
                            //    this.ProcessItem(args, (MediaItem)mediaUploadResult.Item, mediaUploadResult.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Could not save posted file: {file.FileName}", ex, (object)this);
                        throw;
                    }
                }
            }
        }

        private void Upload(HttpPostedFile originalFile, byte[] optimizedImage, bool isUnpack, UploadArgs args, bool isFileBased)
        {
            if (string.Compare(Path.GetExtension(originalFile.FileName), ".zip", StringComparison.InvariantCultureIgnoreCase) == 0 && isUnpack)
            {
                UnpackToDatabase(originalFile, args, isFileBased);
            }
            else
            {
                UploadToDatabase(originalFile.FileName, new MemoryStream(optimizedImage), args, isFileBased);
            }
        }

        private void UnpackToDatabase(HttpPostedFile originalFile, UploadArgs args, bool isFileBased)
        {
            string str = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));

            originalFile.SaveAs(str);

            try
            {
                using (ZipReader zipReader = new ZipReader(str))
                {
                    foreach (ZipEntry entry in zipReader.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            UploadToDatabase(entry.Name, (MemoryStream)entry.GetStream(), args, isFileBased);
                        }
                    }
                }
            }
            finally
            {
                FileUtil.Delete(str);
            }
        }

        private void UploadToDatabase(string fileName, MemoryStream optimizedImage, UploadArgs args, bool isFileBased)
        {
            string validPath = FileUtil.MakePath(args.Folder, Path.GetFileName(fileName), '/');
            string validMediaPath = MediaPathManager.ProposeValidMediaPath(validPath);
            Item createdItem = null;

            MediaCreatorOptions options = new MediaCreatorOptions()
            {
                Versioned = args.Versioned,
                Language = args.Language,
                OverwriteExisting = !args.Overwrite,
                Destination = validMediaPath,
                FileBased = isFileBased,
                AlternateText = args.GetFileParameter(fileName, "alt"),
                Database = null,
            };

            options.Build(GetMediaCreatorOptionsArgs.UploadContext);

            createdItem = MediaManager.Creator.CreateFromStream(optimizedImage, validPath, options);
        }

        private void ProcessItem(UploadArgs args, MediaItem mediaItem, string path)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.ArgumentNotNull((object)mediaItem, "mediaItem");
            Assert.ArgumentNotNull((object)path, "path");
            if (args.Destination == UploadDestination.Database)
                Log.Info("Media Item has been uploaded to database: " + path, (object)this);
            else
                Log.Info("Media Item has been uploaded to file system: " + path, (object)this);
            args.UploadedItems.Add(mediaItem.InnerItem);
        }

        /// <summary>Unpacks to file.</summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        private static void UnpackToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.ArgumentNotNull((object)file, "file");
            string filename = FileUtil.MapPath(TempFolder.GetFilename("temp.zip"));
            file.SaveAs(filename);
            using (ZipReader zipReader = new ZipReader(filename))
            {
                foreach (ZipEntry entry in zipReader.Entries)
                {
                    string str = FileUtil.MakePath(args.Folder, entry.Name, '\\');
                    if (entry.IsDirectory)
                    {
                        Directory.CreateDirectory(str);
                    }
                    else
                    {
                        if (!args.Overwrite)
                            str = FileUtil.GetUniqueFilename(str);
                        Directory.CreateDirectory(Path.GetDirectoryName(str));
                        lock (FileUtil.GetFileLock(str))
                            FileUtil.CreateFile(str, entry.GetStream(), true);
                    }
                }
            }
        }

        /// <summary>Uploads to file.</summary>
        /// <param name="args">The arguments.</param>
        /// <param name="file">The file.</param>
        /// <returns>The name of the uploaded file</returns>
        private string UploadToFile(UploadArgs args, HttpPostedFile file)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.ArgumentNotNull((object)file, "file");
            string str = FileUtil.MakePath(args.Folder, Path.GetFileName(file.FileName), '\\');
            if (!args.Overwrite)
                str = FileUtil.GetUniqueFilename(str);
            file.SaveAs(str);
            Log.Info("File has been uploaded: " + str, (object)this);
            return Assert.ResultNotNull<string>(str);
        }
    }
}