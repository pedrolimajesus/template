using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Raven;
using AppComponents.Web;
using Lok.Unik.ModelCommon.Client;
using log4net;

namespace Shrike.DAL.Manager
{
    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class ContentLibraryManager
    {
        private readonly ILog _log;

        public ContentLibraryManager()
        {
            _log = ClassLogger.Create(this.GetType());
        }

        public IEnumerable<ContentLibrary> GetLibraries()
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from libraries in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>()
                             select libraries;

                    return ToModel(q2.ToArray());
                }
            }
        }

        private IEnumerable<ContentLibrary> ToModel(IEnumerable<Lok.Unik.ModelCommon.Client.ContentLibrary> contentLibrary)
        {
            //return contentLibrary.Select(library => new ContentLibrary
            //{
            //    Id = library.Id,
            //    Title = library.Title,
            //    Description = library.Description,
            //    UserName = library.CreatorUserName,
            //    State = library.State.ToString(),
            //    Tags = ToModelTags(library.Tags),
            //    ContentItems = ToModelItems(library.Files),
            //    CreationDate = library.CreationDate,
            //    Users = ToModelUsers(library.InvitedUsers),
            //    CreatorPrincipalId = library.CreatorPrincipalId
            //}).ToList();
            return new BindingList<ContentLibrary>();
        }

        private ICollection<User> ToModelUsers(IList<Lok.Unik.ModelCommon.Client.User> iList)
        {
            var users = new Collection<User>();
            foreach (var user in iList)
            {
                //var tempUser = new UserManager().GetAll().FirstOrDefault(x => x.Id == user.Id);

                //if (tempUser != null)
                //    users.Add(new User
                //    {
                //        Id = tempUser.Id,
                //        Username = tempUser.AppUser.UserName,
                //        DateCreated = tempUser.AppUser.DateCreated.ToShortDateString(),
                //        Status = tempUser.AppUser.Status
                //    });
            }

            return users;
        }

        private ICollection<ContentItem> ToModelItems(IEnumerable<Lok.Unik.ModelCommon.Client.ContentItem> iList)
        {
            //return iList.Select(items =>
            //{
            //    var firstOrDefault = new ApplicationManager().GetAllAplications().FirstOrDefault(x => x.Id == items.AssociatedApplication);
            //    return firstOrDefault != null ? new ContentItem
            //    {
            //        Id = items.Id,
            //        Title = items.Title,
            //        AssociateApp = items.AssociatedApplication,
            //        ApplicationName = firstOrDefault.Name,
            //        VersionNumber = items.VersionNumber,
            //        LatestVersion = items.LatestVersion,
            //        LibraryId = items.LibraryId.ToString(),
            //        Versions = items.Versions
            //    } : null;
            //}).ToList();
            return new Collection<ContentItem>();
        }

        public ICollection<Tag> ToModelTags(IList<Tag> iList)
        {
            //return iList.Select(tag => new Models.Tag
            //{
            //    Id = tag.Id,
            //    Name = tag.Attribute,
            //    Category = tag.Category.Name,
            //    CreateDate = tag.CreateDate,
            //    Color = tag.Category.Color.ToString(),
            //    Type = tag.Type.ToString()
            //}).ToList();

            return new Collection<Tag>();
        }

        public void CreateLibrary(ContentLibrary contentLibrary, ApplicationUser user, List<string> appIds)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    contentLibrary.Id = Guid.NewGuid();

                    session.Store(ToCommonModel(contentLibrary, user, appIds));
                    session.SaveChanges();

                    _log.InfoFormat("{0} Library has been Stored sucessfully", contentLibrary.Title);
                }
            }
        }

        public Lok.Unik.ModelCommon.Client.ContentLibrary ToCommonModel(ContentLibrary contentLibrary, ApplicationUser user, List<string> appIds)
        {
            //if (contentLibrary.ContentItems.Any())
            //{
            //    for (var i = 0; i < appIds.Count; i++)
            //    {
            //        contentLibrary.ContentItems.ElementAt(i).AssociateApp = new Guid(appIds[i]);
            //    }
            //}

            //var content = new Lok.Unik.ModelCommon.Client.ContentLibrary
            //{
            //    Id = contentLibrary.Id,
            //    Title = contentLibrary.Title,
            //    Description = contentLibrary.Description,
            //    CreationDate = DateTime.UtcNow,
            //    CreatorPrincipalId = user.PrincipalId,
            //    CreatorUserName = user.UserName,
            //    Files = ToCommonItems(contentLibrary.ContentItems, contentLibrary.Id, user),
            //    State = State.Enabled,
            //    BasePath = contentLibrary.BasePath
            //};

            //content.Tags.Add(new TagManager().AddDefault<Lok.Unik.ModelCommon.Client.ContentLibrary>(content.Title, content.Id.ToString()));

            //return content;
            return new ContentLibrary();
        }

        private IList<Lok.Unik.ModelCommon.Client.ContentItem> ToCommonItems(IEnumerable<ContentItem> iCollection, Guid forceId, ApplicationUser user)
        {

            //return iCollection.Select(item => new Lok.Unik.ModelCommon.Client.ContentItem
            //{
            //    Id = item.Id,
            //    Title = item.Title,
            //    LibraryId = forceId,
            //    Description = string.Format("Package for {0}", item.Title),
            //    VersionNumber = 1,
            //    AssociatedApplication = item.AssociateApp,
            //    Versions = new List<ContentItemUpload>
            //                                                          {
            //                                                              new ContentItemUpload
            //                                                                  {
            //                                                                      UploadingUserName = user.UserName,
            //                                                                      UploadingUserPrincipalId = user.PrincipalId,
            //                                                                  }
            //                                                          }
            //}).ToList();
            return new List<ContentItem>();
        }

        public void ChangeState(string id, string state)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var libs = from libraries in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>() select libraries;

                    var lib = libs.ToArray().FirstOrDefault(x => x.Id.ToString() == id);

                    if (lib != null) lib.State = (State)Enum.Parse(typeof(State), state);
                    session.SaveChanges();

                    if (lib != null) _log.InfoFormat("{0} State has been changed to {1}", lib.Title, lib.State.ToString());
                }
            }
        }

        public void DeleteLibrary(string id)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var libs = from q in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>() select q;

                    var lib = libs.ToArray().FirstOrDefault(x => x.Id.ToString() == id);

                    if (lib != null)
                    {
                        foreach (var item in lib.Files)
                        {
                            var relativePath = Path.Combine(lib.BasePath, string.Format("{0}.zip", item.Id));
                            if (File.Exists(relativePath))
                                File.Delete(relativePath);
                            _log.InfoFormat("Deleting {0} stored at {1}", lib.Title, lib.BasePath);
                        }

                    }

                    session.Delete(lib);
                    if (lib != null) _log.InfoFormat("{0} Library has been deleted successfully", lib.Title);
                    session.SaveChanges();
                }
            }
        }

        public void EditLibrary(ContentLibrary contentLibrary)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var libs = from q in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>() select q;

                    var lib = libs.ToArray().FirstOrDefault(x => x.Id == contentLibrary.Id);

                    if (lib != null)
                    {
                        lib.Title = contentLibrary.Title;
                        lib.Description = contentLibrary.Description;
                       // lib.State = (State)Enum.Parse(typeof(State), contentLibrary.State);
                        //..
                        _log.InfoFormat("{0} Library has been updated", lib.Title);
                        session.SaveChanges();
                    }

                }
            }
        }

        public void CreateLibrary(ContentLibrary contentLibrary, ApplicationUser user, List<System.Web.HttpPostedFileBase> fileBases, List<string> appIds)
        {

            //var cf = Catalog.Factory.Resolve<IConfig>();
            //var containerFileShare = cf[ComServerConfiguration.DistributedFileShare];
            //var contentContainerPath = cf[ContentFileStorage.ContentFilesContainer];
            //var containerLibPath = cf[ContentFileStorage.ContentLibrariesContainer];

            //var containerHost = Path.Combine(containerFileShare, contentContainerPath);
            //var directory = Path.Combine(containerHost, containerLibPath);

            //contentLibrary.Id = Guid.NewGuid();
            //contentLibrary.BasePath = directory;

            //foreach (var file in fileBases.Where(file => file.ContentLength != 0))
            //{
            //    if (contentLibrary.ContentItems == null)
            //        contentLibrary.ContentItems = new Collection<ContentItem>();

            //    contentLibrary.ContentItems.Add(new ContentItem
            //    {
            //        Id = Guid.NewGuid(),
            //        CreatorPrincipalId = user.PrincipalId,
            //        Title = file.FileName,
            //        Path = directory,

            //    });

            //}

            //using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            //{
            //    using (var session = DocumentStoreLocator.ContextualResolve())
            //    {
            //        session.Store(ToCommonModel(contentLibrary, user, appIds));
            //        session.SaveChanges();
            //        _log.InfoFormat("{0} Library has been Created", contentLibrary.Title);
            //    }
            //}

            //for (var i = 0; i < contentLibrary.ContentItems.Count; i++)
            //{
            //    var flh = new FileStorageHelper();
            //    byte[] fileData;
            //    var objId = string.Format("{0}.zip", contentLibrary.ContentItems.ElementAt(i).Id);

            //    using (var binaryReader = new BinaryReader(fileBases[i].InputStream))
            //        fileData = binaryReader.ReadBytes(fileBases[i].ContentLength);

            //    flh.SaveData(containerLibPath, objId, fileData);
            //    _log.InfoFormat("{0} library has been stored at '{1}'", objId, directory);
            //}

        }

        public IEnumerable<Lok.Unik.ModelCommon.Client.ContentLibrary> GetAll()
        {

            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = from lib in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>() select lib;

                    return query.ToArray();
                }
            }
        }

        public void UpdateContentTag(Lok.Unik.ModelCommon.Client.ContentLibrary lib)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentLib = (from libraries in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>()
                                      select libraries).ToArray().FirstOrDefault(x => x.Id == lib.Id);
                    if (currentLib != null)
                    {
                        currentLib.Tags = lib.Tags;
                        _log.InfoFormat("{0} Application Tags has been Updated", currentLib.Title);

                    }
                    session.SaveChanges();
                }
            }
        }

        public void DeleteItemPackage(string id)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var libTarget =
                        (from libs in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>()
                         where libs.Files.Any(file => file.Id == Guid.Parse(id))
                         select libs)
                         .FirstOrDefault();

                    if (libTarget != null)
                    {
                        var tempItem = libTarget.Files.FirstOrDefault(file => file.Id == Guid.Parse(id));

                        if (tempItem != null)
                        {
                            var objPath = Path.Combine(libTarget.BasePath, string.Format("{0}.zip", tempItem.Id));
                            if (File.Exists(objPath))
                            {
                                File.Delete(objPath);
                                _log.InfoFormat("'{0}' LibraryItem has been removed from '{1}'", tempItem.Title, objPath);
                            }

                            libTarget.Files.Remove(tempItem);
                        }
                    }

                    session.SaveChanges();
                }
            }
        }

        public void AddItemPackage(ContentLibrary library, List<System.Web.HttpPostedFileBase> tempFiles, List<string> associateAppWithFile, ApplicationUser user)
        {

            //var cf = Catalog.Factory.Resolve<IConfig>();
            //var containerFileShare = cf[ComServerConfiguration.DistributedFileShare];
            //var contentContainerPath = cf[ContentFileStorage.ContentFilesContainer];
            //var containerLibPath = cf[ContentFileStorage.ContentLibrariesContainer];

            //var containerHost = Path.Combine(containerFileShare, contentContainerPath);
            //var directory = Path.Combine(containerHost, containerLibPath);

            //library.BasePath = directory;

            //foreach (var file in tempFiles.Where(file => file.ContentLength != 0))
            //{
            //    if (library.ContentItems == null)
            //        library.ContentItems = new Collection<ContentItem>();

            //    library.ContentItems.Add(new ContentItem
            //    {
            //        Id = Guid.NewGuid(),
            //        Title = file.FileName,
            //        Path = directory,
            //        CreatorPrincipalId = library.CreatorPrincipalId,
            //        LibraryId = library.Id.ToString()
            //    });

            //}

            //for (var i = 0; i < associateAppWithFile.Count; i++)
            //    library.ContentItems.ElementAt(i).AssociateApp = new Guid(associateAppWithFile[i]);

            //using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            //{
            //    using (var session = DocumentStoreLocator.ContextualResolve())
            //    {
            //        var query =
            //            (from libs in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>() select libs)
            //            .ToArray()
            //            .FirstOrDefault(x => x.Id == library.Id);

            //        if (query != null)
            //        {
            //            query.BasePath = library.BasePath;

            //            var commomItems = ToCommonItems(library.ContentItems, query.Id, user);

            //            foreach (var contentItem in commomItems)
            //            {
            //                query.Files.Add(contentItem);
            //                _log.InfoFormat("Adding {0} Item into '{1}' Library", contentItem.Title, query.Title);
            //            }

            //        }

            //        session.SaveChanges();
            //    }
            //}

            //for (var i = 0; i < library.ContentItems.Count; i++)
            //{
            //    var flh = new FileStorageHelper();
            //    byte[] fileData;
            //    var objId = string.Format("{0}.zip", library.ContentItems.ElementAt(i).Id);

            //    using (var binaryReader = new BinaryReader(tempFiles[i].InputStream))
            //        fileData = binaryReader.ReadBytes(tempFiles[i].ContentLength);

            //    flh.SaveData(containerLibPath, objId, fileData);

            //    _log.InfoFormat("Storing {0} into '{1}'", objId, directory);
            //}

            //_log.InfoFormat("Storing Success");

        }

        public void DeleteUserRole(string id)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query =
                        (from libs in session.Query<Lok.Unik.ModelCommon.Client.ContentLibrary>() select libs)
                        .ToArray();

                    var temp = query;

                    for (var i = 0; i < temp.Length; i++)
                    {
                        for (var j = 0; j < temp[i].InvitedUsers.Count; j++)
                        {
                            var users = new UserManager().GetAll();

                            foreach (var user in users)
                            {
                                if (user.Id.ToString().Equals(id))
                                    query[i].InvitedUsers.RemoveAt(j);
                            }

                        }
                    }

                    session.SaveChanges();
                }
            }
        }
    }
}
