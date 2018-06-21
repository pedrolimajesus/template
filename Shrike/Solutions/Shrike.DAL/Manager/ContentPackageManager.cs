using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Raven;
using Lok.Unik.ModelCommon.Client;
using log4net;

namespace Shrike.DAL.Manager
{
    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class ContentPackageManager
    {
        private readonly ILog _log;

        public ContentPackageManager()
        {
            _log = ClassLogger.Create(this.GetType());
        }

        public static readonly string DefaultPackageContainer = @"ContentPackages";

        public IEnumerable<ContentPackage> GetPackages()
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = from cp in session.Query<Lok.Unik.ModelCommon.Client.ContentPackage>() select cp;
                    return ToModel(query.ToArray());
                }
            }
        }

        public IEnumerable<Lok.Unik.ModelCommon.Client.ContentPackage> GetAll()
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = from cp in session.Query<Lok.Unik.ModelCommon.Client.ContentPackage>() select cp;
                    return query.ToArray();
                }
            }
        }

        private IEnumerable<ContentPackage> ToModel(IEnumerable<Lok.Unik.ModelCommon.Client.ContentPackage> contentPackage)
        {
            //return contentPackage.Select(pack => new ContentPackage
            //{
            //    Id = pack.Id,
            //    Name = pack.Name,
            //    Description = pack.Description,
            //    CreationDate = pack.CreationTime,
            //    CreatorPrincipalId = pack.CreatorPrincipalId,
            //    Application = new ApplicationManager().GetApplicationById(new Guid(pack.AssociatedApplication)),
            //    Tags = ToModelTags(pack.Tags)
            //}).ToList();

            return new List<ContentPackage>();
        }

        private ICollection<Tag> ToModelTags(IEnumerable<Tag> iList)
        {
            //return iList.Select(tag => new Models.Tag
            //{
            //    Id = tag.Id,
            //    Name = tag.Attribute,
            //    Type = tag.Type.ToString(),
            //    Category = tag.Category.Name,
            //    Color = tag.Category.Color.ToString(),
            //    CreateDate = tag.CreateDate
            //}).ToList();
            return new Collection<Tag>();
        }

        public void CreatePackage(string userId, string packName, string packDesc, string appId, string[] selecteditems, params string[] portalValues)
        {
            //var libs = new ContentLibraryManager().GetAll();

            //var package = new Lok.Unik.ModelCommon.Client.ContentPackage
            //{
            //    Id = Guid.NewGuid(),
            //    Name = packName,
            //    Description = packDesc,
            //    AssociatedApplication = appId,
            //    CreatorPrincipalId = userId,
            //    AssemblyHistories = ToCommonAssemblyHistories(selecteditems, libs)
            //};

            //package.Tags.Add(new TagManager().AddDefault<Lok.Unik.ModelCommon.Client.ContentPackage>(package.Name, package.Id.ToString()));

            //var cf = Catalog.Factory.Resolve<IConfig>();
            //var containerFileShare = cf[ComServerConfiguration.DistributedFileShare];
            //var contentContainerPath = cf[ContentFileStorage.ContentFilesContainer];
            //var contentLibraryPath = cf[ContentFileStorage.ContentLibrariesContainer];
            //var containerPackagePath = cf[ContentFileStorage.ContentPackagesContainer];

            //var containerHost = Path.Combine(containerFileShare, contentContainerPath);
            //var directory = Path.Combine(containerHost, containerPackagePath);

            //if (!Directory.Exists(directory))
            //    Directory.CreateDirectory(directory);

            //package.ConfigurationRelativePath = directory;

            //using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            //{
            //    using (var session = DocumentStoreLocator.ContextualResolve())
            //    {
            //        session.Store(package);
            //        session.SaveChanges();
            //        _log.InfoFormat("{0} Package has been created succesfully and stored at '{1}'", package.Name, directory);
            //    }
            //}

            //var inputContentFilePaths = (from item in package.AssemblyHistories
            //                             where selecteditems.Contains(item.ContentFileId.ToString())
            //                             let helperLibDir = Path.Combine(containerHost, contentLibraryPath)
            //                             select Path.Combine(helperLibDir, string.Format("{0}.zip", item.ContentFileId))).ToList();

            //var applicationManager = new ApplicationManager();
            //var app = applicationManager.GetApplicationById(Guid.Parse(appId));
            //var packageAssemblerFactory = Catalog.Factory.Resolve<IContentPackageAssemblerFactory>();

            //var assembler = packageAssemblerFactory.GetContentPackageAssembler(app);
            //assembler.Parameters.Clear();

            //assembler.Parameters.Add("ScreenForm", portalValues[0]);
            //assembler.Parameters.Add("Layout", portalValues[1]);

            //var mergedPackagePath = Path.Combine(package.ConfigurationRelativePath, string.Format("{0}.zip", package.Id));

            //assembler.Merge(mergedPackagePath, inputContentFilePaths);
            //_log.InfoFormat("{0} PackageAssemble is already in '{1}'", package.Name, mergedPackagePath);
        }

        private static List<ContentAssemblyHistory> ToCommonAssemblyHistories(IEnumerable<string> selecteditems, IEnumerable<ContentLibrary> libs)
        {
            var histories = new List<ContentAssemblyHistory>();

            foreach (var contentLib in libs)
            {
                histories.AddRange(from item in contentLib.Files
                                   where selecteditems.Contains(item.Id.ToString())
                                   select new ContentAssemblyHistory
                                   {
                                       ContentFileId = item.Id,
                                       ContentFileTitle = item.Title,
                                       ContentFileDescription = item.Description,
                                       ContentFileVersion = item.VersionNumber.ToString(CultureInfo.InvariantCulture),
                                       ContentLibraryId = item.LibraryId,
                                       ContentLibraryTitle = libs.FirstOrDefault(x => x.Id == item.LibraryId).Title,
                                   });
            }

            return histories;
        }

        public void DeletePackage(string id)
        {
            using (var cntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query =
                        (from packs in session.Query<Lok.Unik.ModelCommon.Client.ContentPackage>() select packs)
                        .ToArray()
                        .FirstOrDefault(x => x.Id.ToString() == id);

                    session.Delete(query);
                    session.SaveChanges();

                    if (query != null)
                    {
                        var objPath = Path.Combine(query.ConfigurationRelativePath, string.Format("{0}.zip", query.Id));
                        if (File.Exists(objPath))
                        {
                            File.Delete(objPath);
                            _log.InfoFormat("{0} has been deleted and removed from '{1}'", query.Name, objPath);
                        }
                    }

                }
            }
        }

        public IEnumerable<Tag> GetTagsFromContentPackage()
        {
            var tags = new List<Tag>();

            using (var ntx = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query =
                        (from packages in session.Query<Lok.Unik.ModelCommon.Client.ContentPackage>() select packages).
                            ToArray();

                    //foreach (var contentPackage in query)
                    //{
                    //    tags.AddRange(contentPackage.Tags.Select(tag => new Models.Tag
                    //    {
                    //        Id = tag.Id,
                    //        Name = tag.Attribute,
                    //        Type = tag.Type.ToString(),
                    //        Category = tag.Category.Name,
                    //        CreateDate = tag.CreateDate,
                    //        Color = tag.Category.Color.ToString()
                    //    }));
                    //}

                    return tags;
                }
            }
        }

        public void UpdateTagForPackage(Lok.Unik.ModelCommon.Client.ContentPackage contentPackage)
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var currentAppl = (from packs in session.Query<Lok.Unik.ModelCommon.Client.ContentPackage>()
                                       select packs).ToArray().FirstOrDefault(x => x.Id == contentPackage.Id);
                    if (currentAppl != null)
                    {
                        currentAppl.Tags = contentPackage.Tags;
                        _log.InfoFormat("Package Tags Updated for {0}", currentAppl.Name);

                    }
                    session.SaveChanges();

                }
            }
        }

        /// <summary>
        /// Get Packages by ContentManager Role
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <returns></returns>
        public IEnumerable<ContentPackage> GetPackages(string userId)
        {

            using (var cntxt = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var query = (from packs in session.Query<Lok.Unik.ModelCommon.Client.ContentPackage>() select packs)
                        .ToArray()
                        .Where(x => x.CreatorPrincipalId == userId).ToArray();

                    return ToModel(query);
                }
            }
        }
    }
}
