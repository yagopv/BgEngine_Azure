//==============================================================================
// This file is part of BgEngine.
//
// BgEngine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// BgEngine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with BgEngine. If not, see <http://www.gnu.org/licenses/>.
//==============================================================================
// Copyright (c) 2011 Yago Pérez Vázquez
// Version: 1.0
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Web;
using System.Web.Helpers;

using BgEngine.Domain.EntityModel;
using BgEngine.Domain.RepositoryContracts;
using BgEngine.Application.DTO;
using PagedList;
using BgEngine.Application.ResourceConfiguration;

//Azure
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace BgEngine.Application.Services
{
    public class MediaServices : IMediaServices
    {
        IImageRepository ImageRepository;
        IAlbumRepository AlbumRepository;
        IVideoRepository VideoRepository;
        ITagRepository TagRepository;
        CloudStorageAccount StorageAccount;
        /// <summary>
        /// ctor
        /// </summary>
        public MediaServices(IImageRepository imagerepository, IAlbumRepository albumrepository, IVideoRepository videorepository, ITagRepository tagrepository)
        {
            this.ImageRepository = imagerepository;
            this.AlbumRepository = albumrepository;
            this.VideoRepository = videorepository;
            this.TagRepository = tagrepository;
            // Parse the connection string to accessing Storage account defined for BgEngine
            StorageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
        }
        /// <summary>
        /// Add new Image to database
        /// </summary>
        /// <param name="entity">Image to add</param>
        public void AddImage(Image entity)
        {
            ImageRepository.Insert(entity);
            ImageRepository.UnitOfWork.Commit();
        }
        /// <summary>
        /// Get images searching in Title and Description
        /// </summary>
        /// <typeparam name="TKey">Order Key</typeparam>
        /// <param name="orderByExpression">Order expression</param>
        /// <param name="searchstring">Search string</param>
        /// <returns>List of Images</returns>
        public IEnumerable<Image> SearchForImagesByParam<TKey>(Expression<Func<Image, TKey>> orderByExpression, string searchstring = "")
        {
            return ImageRepository.GetImages(orderByExpression, searchstring);
        }
        /// <summary>
        /// Delete an Image from Database and remove it from Cloud Storage
        /// </summary>
        /// <param name="id">Identity of the Image to Delete</param>
        /// <returns>If deletion is completed</returns>
        public bool DeleteImageFromDatabaseAndStorage(int id)
        {
            Image image = ImageRepository.GetByID(id);
            try
            {
                ImageRepository.Delete(id);
                ImageRepository.UnitOfWork.Commit();
            }
            catch (Exception ex)
            {
                return false;
            }
            bool imagedeleted = DeleteImageFromStorage(image.Path, image.ThumbnailPath);
            if (!imagedeleted)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Delete an album
        /// </summary>
        /// <param name="albumid">Identity for the Album</param>
        /// <param name="deleterelated">If delete all related images or keep them</param>
        public void DeleteAlbum(int albumid, bool deleterelated)
        {
            Album album = AlbumRepository.GetByID(albumid);
            foreach (Image im in album.Images.ToList())
            {
                if (deleterelated == true)
                {
                    bool imagedeleted = DeleteImageFromStorage(im.Path, im.ThumbnailPath);
                    if (imagedeleted)
                    {
                        ImageRepository.Delete(im.ImageId);
                    }
                }
                else
                {
                    im.AlbumId = null;
                }
            }
            CloudBlobContainer cloudblobcontainer = GetContainerForAlbum(albumid);
            cloudblobcontainer.Delete();
            ImageRepository.UnitOfWork.Commit();
            AlbumRepository.Delete(albumid);
            AlbumRepository.UnitOfWork.Commit();

        }
        /// <summary>
        /// Get a List of images 
        /// </summary>
        /// <param name="albumid">Album to get</param>
        /// <param name="url">url of the image</param>
        /// <returns>List of Images</returns>
        public List<ImageDTO> BuildGalleriaForAlbum(int albumid)
        {
            IEnumerable<Image> images = ImageRepository.Get(i => i.AlbumId == albumid);
            List<ImageDTO> imagesJSON = new List<ImageDTO>();
            foreach (var image in images)
            {
                ImageDTO imageJSON = new ImageDTO
                {
                    image = image.Path,
                    thumb = image.ThumbnailPath,
                    big = image.Path,
                    title = image.Name,
                    description = image.Description
                };
                imagesJSON.Add(imageJSON);
            }
            return imagesJSON;
        }
        /// <summary>
        /// Get Images for select in a jquery autocomplete box
        /// </summary>
        /// <param name="searchstring">The striing to search</param>
        /// <returns>Lisst of Image Titles</returns>
        public List<StringValueDTO> BuildImageAutocompleteSuggestions(string searchstring)
        {
            List<StringValueDTO> imagesJSON = new List<StringValueDTO>();
            foreach (Image image in ImageRepository.Get(i => i.FileName.Contains(searchstring) || i.Description.Contains(searchstring)).ToList())
            {
                imagesJSON.Add(new StringValueDTO(image.FileName));
            }
            return imagesJSON;
        }
        /// <summary>
        /// Upload a list of Images to the Storage
        /// </summary>
        /// <param name="files">Files to Upload</param>
        /// <param name="request">Request base</param>
        /// <param name="albumid">The album of the Images</param>
        /// <returns>Files uploaded</returns>
        public object UploadFileToStorage(ICollection<HttpPostedFileBase> files, HttpRequestBase request, int? albumid)
        {
            int fileUploadedCount = 0;
            string[] filesUploaded = new string[request.Files.Count];
            int i = 0;
            foreach (var file in files)
            {
                if ((file != null) && (file.ContentLength > 0))
                {
                    string imagepath;
                    string thumbnailpath;
                    Guid unique = Guid.NewGuid();
                    if (file.FileName.ToUpper().Contains("JPG") || file.FileName.ToUpper().Contains("JPEG") || file.FileName.ToUpper().Contains("GIF") || file.FileName.ToUpper().Contains("PNG"))
                    {
                        Album album;
                        if (albumid == null)
                        {
                            album = AlbumRepository.Get(a => a.Name == Resources.AppMessages.Default_Album_Name).FirstOrDefault();
                            if (album == null)
                            {
                                album = new Album { Name = Resources.AppMessages.Default_Album_Name, Description = Resources.AppMessages.Default_Album_Description, IsPublic = true, DateCreated = DateTime.Now };
                                AlbumRepository.Insert(album);
                                AlbumRepository.UnitOfWork.Commit(); ;
                                AlbumRepository.Get(a => a.Name == album.Name);
                            }
                            albumid = album.AlbumId;
                        }
                        else
                        {
                            album = AlbumRepository.GetByID(albumid);
                        }
                        CloudBlobContainer container = GetContainerForAlbum(albumid);
                        WebImage image = new WebImage(file.InputStream);
                        image.Resize(1024, 768, preserveAspectRatio: true, preventEnlarge: true)
                             .Crop(1, 1);
                        CloudBlob blob = container.GetBlobReference(unique + "_" + file.FileName);
                        blob.UploadByteArray(image.GetBytes());
                        imagepath = blob.Uri.AbsoluteUri;

                        image.Resize(Int32.Parse(BgResources.Media_ThumbnailWidth), Int32.Parse(BgResources.Media_ThumbnailHeight), preserveAspectRatio: true, preventEnlarge: true)
                             .Crop(1, 1);
                        CloudBlob thblob = container.GetBlobReference(unique + "_th" + file.FileName);
                        thblob.UploadByteArray(image.GetBytes());
                        thumbnailpath = thblob.Uri.AbsoluteUri;

                        ImageRepository.Insert(new Image { Name = file.FileName, Path = imagepath, ThumbnailPath = thumbnailpath, Album = album, DateCreated = DateTime.Now, FileName = file.FileName });
                        ImageRepository.UnitOfWork.Commit();
                        fileUploadedCount++;
                        filesUploaded[i] = file.FileName;
                    }
                    else
                    {
                        CloudBlobContainer container = GetContainerForAlbum(null);
                        CloudBlob blobfile = container.GetBlobReference(unique + "_" + file.FileName);
                        blobfile.UploadFromStream(file.InputStream);
                        fileUploadedCount++;
                        filesUploaded[i] = file.FileName;
                    }
                }
                i++;
            }
            var result = new
            {
                fileCount = fileUploadedCount,
                files = filesUploaded
            };
            return result;
        }
        /// <summary>
        /// Upload one image to album and Storage
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="albumid">The Album Identity</param>
        public void UploadFileToAlbum(HttpPostedFileBase file, int? albumid)
        {
            if ((file != null) && (file.ContentLength > 0))
            {
                string imagepath;
                string thumbnailpath;
                Guid unique = Guid.NewGuid();
                Album album = null;
                if (file.FileName.ToUpper().Contains("JPG") || file.FileName.ToUpper().Contains("JPEG") || file.FileName.ToUpper().Contains("GIF") || file.FileName.ToUpper().Contains("PNG"))
                {
                    if (albumid == null)
                    {
                        album = AlbumRepository.Get(a => a.Name == Resources.AppMessages.Default_Album_Name).FirstOrDefault();
                        if (album == null)
                        {
                            album = new Album { Name = Resources.AppMessages.Default_Album_Name, Description = Resources.AppMessages.Default_Album_Description, IsPublic = true, DateCreated = DateTime.Now };
                            AlbumRepository.Insert(album);
                            AlbumRepository.UnitOfWork.Commit(); ;
                            AlbumRepository.Get(a => a.Name == album.Name);
                        }
                        albumid = album.AlbumId;
                    }
                    else
                    {
                        album = AlbumRepository.GetByID(albumid);
                    }
                    CloudBlobContainer container = GetContainerForAlbum(albumid);
                    WebImage image = new WebImage(file.InputStream);
                    image.Resize(1024, 768, preserveAspectRatio: true, preventEnlarge: true)
                         .Crop(1, 1);
                    CloudBlob blob = container.GetBlobReference(unique + "_" + file.FileName);
                    blob.UploadByteArray(image.GetBytes());
                    imagepath = blob.Uri.AbsoluteUri;

                    image.Resize(Int32.Parse(BgResources.Media_ThumbnailWidth), Int32.Parse(BgResources.Media_ThumbnailHeight), preserveAspectRatio: true, preventEnlarge: true)
                         .Crop(1, 1);
                    CloudBlob thblob = container.GetBlobReference(unique + "_th" + file.FileName);
                    thblob.UploadByteArray(image.GetBytes());
                    thumbnailpath = thblob.Uri.AbsoluteUri;

                    ImageRepository.Insert(new Image { Name = file.FileName, Path = imagepath, ThumbnailPath = thumbnailpath, Album = album, DateCreated = DateTime.Now, FileName = file.FileName });
                    ImageRepository.UnitOfWork.Commit();
                }
                else
                {
                    CloudBlobContainer container = GetContainerForAlbum(null);
                    CloudBlob blobfile = container.GetBlobReference(unique + "_" + file.FileName);
                    blobfile.UploadFromStream(file.InputStream);
                }
            }
        }
        /// <summary>
        /// Check if the images directories exists and create them if necessary
        /// </summary>
        /// <param name="albumid"></param>
        private CloudBlobContainer GetContainerForAlbum(int? albumid)
        {
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container;
            // Retrieve a reference to a container 
            if (albumid == null)
            {
                container = blobClient.GetContainerReference("files");
            }
            else
            {
                container = blobClient.GetContainerReference("album-" + albumid.ToString());
            }     

            // Create the container if it doesn't already exist. If created set access to public
            if (container.CreateIfNotExist())
            {
                container.SetPermissions(
                    new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }
            return container;
        }

        /// <summary>
        /// Get Videos for premium or normal user
        /// </summary>
        /// <param name="ispremium">Id the user has Premium account</param>
        /// <param name="pageindex">The index of the page</param>
        /// <param name="searchstring">The string to search</param>
        /// <returns>Paged List of Videos</returns>
        public IPagedList<Video> FindVideosForRole(bool ispremium, int pageindex, string searchstring)
        {
            if (ispremium)
            {
                if (String.IsNullOrEmpty(searchstring))
                {
                    return VideoRepository.Get(null, v => v.OrderBy(o => o.DateCreated), null).ToPagedList(pageindex, Int32.Parse(BgResources.Pager_SearchVideosPerPage));
                }
                else
                {
                    return VideoRepository.Get(v => v.Name.Contains(searchstring) || v.Description.Contains(searchstring), v => v.OrderBy(o => o.DateCreated), null).ToPagedList(pageindex, Int32.Parse(BgResources.Pager_SearchVideosPerPage));
                }
            }
            else
            {
                if (String.IsNullOrEmpty(searchstring))
                {
                    return VideoRepository.Get(v => v.IsPublic, v => v.OrderBy(o => o.DateCreated), null).ToPagedList(pageindex, Int32.Parse(BgResources.Pager_SearchVideosPerPage));
                }
                else
                {
                    return VideoRepository.Get(v => v.IsPublic && (v.Name.Contains(searchstring) || v.Description.Contains(searchstring)), v => v.OrderBy(o => o.DateCreated), null).ToPagedList(pageindex, Int32.Parse(BgResources.Pager_SearchVideosPerPage));
                }
            }
        }
        /// <summary>
        /// Get Video Titles for jquery autocomplete box
        /// </summary>
        /// <param name="searchstring">The searchstring</param>
        /// <returns>A List of Videos</returns>
        public List<StringValueDTO> BuildVideoAutocompleteSuggestions(string searchstring, bool ispremium)
        {
            List<StringValueDTO> videosJSON = new List<StringValueDTO>();
            if (ispremium)
            {
                foreach (Video video in VideoRepository.Get(v => v.Name.Contains(searchstring) || v.Description.Contains(searchstring)).ToList())
                {
                    videosJSON.Add(new StringValueDTO(video.Name));
                }
            }
            else
            {
                foreach (Video video in VideoRepository.Get(v => (v.Name.Contains(searchstring) || v.Description.Contains(searchstring)) && v.IsPublic).ToList())
                {
                    videosJSON.Add(new StringValueDTO(video.Name));
                }
            }
            return videosJSON;
        }
        /// <summary>
        /// Get images for premium or normal users
        /// </summary>
        /// <param name="ispremium">If the User has Premium account</param>
        /// <returns>List of Images</returns>
        public IEnumerable<Image> FindImagesForRole(bool ispremium)
        {
            if (ispremium)
            {
                return ImageRepository.Get();
            }
            else
            {
                return ImageRepository.Get(i => i.IsPublic);
            }
        }
        /// <summary>
        /// Get Albums for premium or normal users
        /// </summary>
        /// <param name="ispremium">If the User has Premium account</param>
        /// <returns>List of Albums</returns>
        public IEnumerable<Album> FindAlbumsForRole(bool ispremium)
        {
            if (ispremium)
            {
                return AlbumRepository.Get();
            }
            else
            {
                return AlbumRepository.Get(i => i.IsPublic);
            }
        }
        /// <summary>
        /// Private method for Image Deletion in Storage
        /// </summary>
        /// <param name="path">Path of Image</param>
        /// <param name="thumbnailpath">Thumb path of Image</param>
        /// <returns></returns>
        private bool DeleteImageFromStorage(string path, string thumbnailpath)
        {
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();
            CloudBlob blobpath = blobClient.GetBlobReference(path);
            CloudBlob blobthpath = blobClient.GetBlobReference(thumbnailpath);
            return blobpath.DeleteIfExists() && blobthpath.DeleteIfExists();
        }

        /// <summary>
        /// Get latest videos for home page
        /// </summary>
        /// <param name="howmany">How many videos to get</param>
        /// <param name="ispremium">if the user sending the request is premium user or not</param>
        /// <returns>The latest videos added</returns>
        public IEnumerable<Video> FindLatestVideos(int howmany, bool ispremium, string tag, string category)
        {
            if (ispremium)
            {
                if (!String.IsNullOrEmpty(category))
                {
                    return VideoRepository.Get(v => v.Category.Name == category, i => i.OrderByDescending(v => v.DateCreated), null).Take(howmany);
                }
                else
                {
                    if (!String.IsNullOrEmpty(tag))
                    {
                        Tag tagtoselect = TagRepository.Get(t => t.TagName == tag, null, "Videos").FirstOrDefault();
                        if (tagtoselect.Videos != null)
                        {
                            return tagtoselect.Videos.OrderByDescending(v => v.DateCreated).Take(howmany);
                        }
                    }
                    return VideoRepository.Get(null, i => i.OrderByDescending(v => v.DateCreated), null).Take(howmany);
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(category))
                {
                    return VideoRepository.Get(v => v.IsPublic && v.Category.Name == category, i => i.OrderByDescending(v => v.DateCreated), null).Take(howmany);
                }
                else
                {
                    if (!String.IsNullOrEmpty(tag))
                    {
                        Tag tagtoselect = TagRepository.Get(t => t.TagName == tag, null, null).FirstOrDefault();
                        if (tagtoselect != null)
                        {
                            return VideoRepository.Get(v => v.IsPublic && v.Tags.Contains(tagtoselect), i => i.OrderByDescending(v => v.DateCreated), null).Take(howmany);
                        }
                    }
                    return VideoRepository.Get(v => v.IsPublic, i => i.OrderByDescending(v => v.DateCreated), null).Take(howmany);
                }
            }
        }
        /// <summary>
        /// Add a Video to the Database
        /// </summary>
        /// <param name="post">Video</param>
        /// <param name="selectedtags">Tags related with the Video entity</param>
        public void CreateVideo(Video video, int[] tags)
        {
            VideoRepository.Insert(video);
            VideoRepository.AddTagsToVideo(video, tags);
            VideoRepository.UnitOfWork.Commit(); ;
        }
        /// <summary>
        /// Update ans save Video to the Database
        /// </summary>
        /// <param name="post">The Video to save</param>
        /// <param name="tags">Related Tags with the Video entity</param>
        public void UpdateVideo(Video video, int[] tags)
        {
            VideoRepository.Update(video);
            VideoRepository.AddTagsToVideo(video, tags);
            VideoRepository.UnitOfWork.Commit(); ;
        }
    }
}
