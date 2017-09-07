﻿
using BExIS.Dim.Entities.Mapping;
using BExIS.Dim.Entities.Publication;
using BExIS.Dim.Helpers;
using BExIS.Dim.Services;
using BExIS.Dlm.Entities.MetadataStructure;
using BExIS.Dlm.Services.MetadataStructure;
using BExIS.Modules.Dim.UI.Helper;
using BExIS.Security.Entities.Objects;
using BExIS.Security.Services.Objects;
using BExIS.Xml.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BExIS.Modules.Dim.UI.Helpers
{
    public class DimSeedDataGenerator
    {
        public static void GenerateSeedData()
        {


            #region SECURITY

            //workflows = größere sachen, vielen operation
            //operations = einzelne actions

            //1.controller -> 1.Operation


            FeatureManager featureManager = new FeatureManager();

            Feature DataDissemination =
                featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Data Dissemination"));
            if (DataDissemination == null)
                DataDissemination = featureManager.Create("Data Dissemination", "Data Dissemination");

            Feature Mapping = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Mapping"));
            if (Mapping == null) Mapping = featureManager.Create("Mapping", "Mapping", DataDissemination);

            Feature Submission = featureManager.FeatureRepository.Get().FirstOrDefault(f => f.Name.Equals("Submission"));
            if (Submission == null) Submission = featureManager.Create("Submission", "Submission", DataDissemination);

            OperationManager operationManager = new OperationManager();


            #region Help Workflow

            operationManager.Create("DIM", "Help", "*", DataDissemination);

            #endregion

            #region Admin Workflow

            operationManager.Create("Dim", "Admin", "*", DataDissemination);

            operationManager.Create("Dim", "Submission", "*", Submission);
            operationManager.Create("Dim", "Mapping", "*", Mapping);


            #endregion

            #region Mapping Workflow

            //ToDo add security after Refactoring DIM mapping workflow


            //workflow = new Workflow();
            //workflow.Name = "Mapping";
            //workflowManager.Create(workflow);

            //operation = operationManager.Create("Dim", "Admin", "*", null, workflow);
            //workflow.Operations.Add(operation);

            //Mapping.Workflows.Add(workflow);

            #endregion

            #region Submission Workflow

            //ToDo add security after Refactoring DIM Submission workflow

            //workflow = new Workflow();
            //workflow.Name = "Submission";
            //workflowManager.Create(workflow);

            //operation = operationManager.Create("Dim", "Admin", "*", null, workflow);
            //workflow.Operations.Add(operation);

            //Submission.Workflows.Add(workflow);

            #endregion

            #endregion

            #region EXPORT

            SubmissionManager submissionManager = new SubmissionManager();
            submissionManager.Load();

            createMetadataStructureRepoMaps();


            #endregion

            #region MAPPING

            createMappings();

            #endregion

            //ImportPartyTypes();


        }

        private static void createMetadataStructureRepoMaps()
        {
            PublicationManager publicationManager = new PublicationManager();

            //set MetadataStructureToRepository for gbif and pensoft
            long metadataStrutcureId = 0;
            long repositoryId = 0;

            //get id of metadatstructure
            MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
            string metadatStrutcureName = "gbif";
            if (metadataStructureManager.Repo.Get().Any(m => m.Name.ToLower().Equals(metadatStrutcureName)))
            {
                MetadataStructure metadataStructure =
                    metadataStructureManager.Repo.Get()
                        .FirstOrDefault(m => m.Name.ToLower().Equals(metadatStrutcureName));
                if (metadataStructure != null)
                {
                    metadataStrutcureId = metadataStructure.Id;
                }
            }

            //get id of metadatstructure
            string repoName = "pensoft";
            if (publicationManager.RepositoryRepo.Get().Any(m => m.Name.ToLower().Equals(repoName)))
            {
                Repository repository =
                    publicationManager.RepositoryRepo.Get().FirstOrDefault(m => m.Name.ToLower().Equals(repoName));
                if (repository != null)
                {
                    repositoryId = repository.Id;
                }
            }

            if (metadataStrutcureId > 0 && repositoryId > 0)
            {
                publicationManager.CreateMetadataStructureToRepository(metadataStrutcureId, repositoryId);
            }
        }

        private static void createMappings()
        {
            createSystemKeyMappings();
        }

        private static void createSystemKeyMappings()
        {
            MetadataStructureManager metadataStructureManager = new MetadataStructureManager();
            MappingManager mappingManager = new MappingManager();
            XmlMetadataWriter xmlMetadataWriter = new XmlMetadataWriter(XmlNodeMode.xPath);

            //#region ABCD BASIC
            if (metadataStructureManager.Repo.Get().Any(m => m.Name.ToLower().Equals("basic abcd")))
            {
                MetadataStructure metadataStructure =
                    metadataStructureManager.Repo.Get().FirstOrDefault(m => m.Name.ToLower().Equals("basic abcd"));

                XDocument metadataRef = xmlMetadataWriter.CreateMetadataXml(metadataStructure.Id);


                //create root mapping
                LinkElement abcdRoot = createLinkELementIfNotExist(mappingManager, metadataStructure.Id, metadataStructure.Name, LinkElementType.MetadataStructure, LinkElementComplexity.None);

                //create system mapping
                LinkElement system = createLinkELementIfNotExist(mappingManager, 0, "System", LinkElementType.System, LinkElementComplexity.None);

                #region mapping ABCD BASIC to System Keys


                Mapping rootTo = mappingManager.CreateMapping(abcdRoot, system, 0, null, null);
                Mapping rootFrom = mappingManager.CreateMapping(system, abcdRoot, 0, null, null);

                createToKeyMapping("Title", LinkElementType.MetadataNestedAttributeUsage, "Title", LinkElementType.MetadataNestedAttributeUsage, Key.Title, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("Title", LinkElementType.MetadataNestedAttributeUsage, "Title", LinkElementType.MetadataNestedAttributeUsage, Key.Title, rootFrom, metadataRef, mappingManager);

                createToKeyMapping("Details", LinkElementType.MetadataNestedAttributeUsage, "MetadataDescriptionRepr", LinkElementType.ComplexMetadataAttribute, Key.Description, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("Details", LinkElementType.MetadataNestedAttributeUsage, "MetadataDescriptionRepr", LinkElementType.ComplexMetadataAttribute, Key.Description, rootFrom, metadataRef, mappingManager);

                createToKeyMapping("FullName", LinkElementType.MetadataNestedAttributeUsage, "PersonName", LinkElementType.MetadataNestedAttributeUsage, Key.Author, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("FullName", LinkElementType.MetadataNestedAttributeUsage, "PersonName", LinkElementType.MetadataNestedAttributeUsage, Key.Author, rootFrom, metadataRef, mappingManager);

                createToKeyMapping("Text", LinkElementType.MetadataNestedAttributeUsage, "License", LinkElementType.MetadataNestedAttributeUsage, Key.License, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("Text", LinkElementType.MetadataNestedAttributeUsage, "License", LinkElementType.MetadataNestedAttributeUsage, Key.License, rootFrom, metadataRef, mappingManager);


                #endregion
            }



            //#endregion


            #region mapping GBIF to System Keys

            if (metadataStructureManager.Repo.Get().Any(m => m.Name.ToLower().Equals("gbif")))
            {
                MetadataStructure metadataStructure =
                    metadataStructureManager.Repo.Get().FirstOrDefault(m => m.Name.ToLower().Equals("gbif"));

                XDocument metadataRef = xmlMetadataWriter.CreateMetadataXml(metadataStructure.Id);


                //create root mapping
                LinkElement gbifRoot = createLinkELementIfNotExist(mappingManager, metadataStructure.Id, metadataStructure.Name, LinkElementType.MetadataStructure, LinkElementComplexity.None);

                //create system mapping
                LinkElement system = createLinkELementIfNotExist(mappingManager, 0, "System", LinkElementType.System, LinkElementComplexity.None);

                #region mapping GBIF to System Keys


                Mapping rootTo = mappingManager.CreateMapping(gbifRoot, system, 0, null, null);
                Mapping rootFrom = mappingManager.CreateMapping(system, gbifRoot, 0, null, null);

                createToKeyMapping("title", LinkElementType.MetadataNestedAttributeUsage, "Basic", LinkElementType.MetadataPackageUsage, Key.Title, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("title", LinkElementType.MetadataNestedAttributeUsage, "Basic", LinkElementType.MetadataPackageUsage, Key.Title, rootFrom, metadataRef, mappingManager);

                createToKeyMapping("para", LinkElementType.MetadataNestedAttributeUsage, "abstract", LinkElementType.MetadataPackageUsage, Key.Description, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("para", LinkElementType.MetadataNestedAttributeUsage, "abstract", LinkElementType.MetadataPackageUsage, Key.Description, rootFrom, metadataRef, mappingManager);



                createToKeyMapping("givenName", LinkElementType.MetadataNestedAttributeUsage, "Metadata/creator/creatorType/individualName", LinkElementType.MetadataAttributeUsage, Key.Author, rootTo, metadataRef, mappingManager, mappingManager.CreateTransformationRule("", "givenName[0] surName[0]"));
                createToKeyMapping("givenName", LinkElementType.MetadataNestedAttributeUsage, "Metadata/creator/creatorType/individualName", LinkElementType.MetadataAttributeUsage, Key.Author, rootFrom, metadataRef, mappingManager, mappingManager.CreateTransformationRule(@"\w+", "Author[0]"));

                createToKeyMapping("surName", LinkElementType.MetadataNestedAttributeUsage, "Metadata/creator/creatorType/individualName", LinkElementType.MetadataAttributeUsage, Key.Author, rootTo, metadataRef, mappingManager, mappingManager.CreateTransformationRule("", "givenName[0] surName[0]"));
                createToKeyMapping("surName", LinkElementType.MetadataNestedAttributeUsage, "Metadata/creator/creatorType/individualName", LinkElementType.MetadataAttributeUsage, Key.Author, rootFrom, metadataRef, mappingManager, mappingManager.CreateTransformationRule(@"\w+", "Author[1]"));


                createToKeyMapping("title", LinkElementType.MetadataNestedAttributeUsage, "project", LinkElementType.MetadataPackageUsage, Key.ProjectTitle, rootTo, metadataRef, mappingManager);
                createFromKeyMapping("title", LinkElementType.MetadataNestedAttributeUsage, "project", LinkElementType.MetadataPackageUsage, Key.ProjectTitle, rootFrom, metadataRef, mappingManager);

                #endregion
            }

            #endregion
        }

        /// <summary>
        /// Map a node from xml to system key
        /// </summary>
        /// <param name="simpleNodeName">name or xpath</param>
        /// <param name="simpleType"></param>
        /// <param name="complexNodeName">name or xpath</param>
        /// <param name="complexType"></param>
        /// <param name="key"></param>
        /// <param name="root"></param>
        /// <param name="metadataRef"></param>
        /// <param name="mappingManager"></param>
        private static void createToKeyMapping(
            string simpleNodeName, LinkElementType simpleType,
            string complexNodeName, LinkElementType complexType,
            Key key,
            Mapping root,
            XDocument metadataRef,
            MappingManager mappingManager, TransformationRule transformationRule = null)
        {

            if (transformationRule == null) transformationRule = new TransformationRule();

            LinkElement le = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(key),
                    key.ToString(), LinkElementType.Key, LinkElementComplexity.Simple);

            if (simpleNodeName.Equals(complexNodeName))
            {

                List<XElement> elements = getXElements(simpleNodeName, metadataRef);

                foreach (XElement xElement in elements)
                {
                    string sId = xElement.Attribute("id").Value;
                    string name = xElement.Attribute("name").Value;
                    LinkElement tmp = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(sId), name,
                        simpleType, LinkElementComplexity.Simple);

                    Mapping tmpMapping = MappingHelper.CreateIfNotExistMapping(tmp, le, 1, new TransformationRule(), root);
                    MappingHelper.CreateIfNotExistMapping(tmp, le, 2, transformationRule, tmpMapping);
                }
            }
            else
            {
                IEnumerable<XElement> complexElements = getXElements(complexNodeName, metadataRef);

                foreach (var complex in complexElements)
                {
                    string sIdComplex = complex.Attribute("id").Value;
                    string nameComplex = complex.Attribute("name").Value;
                    LinkElement tmpComplexElement = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(sIdComplex), nameComplex,
                        complexType, LinkElementComplexity.Complex);

                    Mapping complexMapping = MappingHelper.CreateIfNotExistMapping(tmpComplexElement, le, 1, new TransformationRule(), root);


                    IEnumerable<XElement> simpleElements = XmlUtility.GetAllChildren(complex).Where(s => s.Name.LocalName.Equals(simpleNodeName));

                    foreach (XElement xElement in simpleElements)
                    {
                        string sId = xElement.Attribute("id").Value;
                        string name = xElement.Attribute("name").Value;
                        LinkElement tmp = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(sId), name,
                            simpleType, LinkElementComplexity.Simple);

                        MappingHelper.CreateIfNotExistMapping(tmp, le, 2, transformationRule, complexMapping);
                    }
                }


            }
        }

        /// <summary>
        /// Map a system key to xml node
        /// </summary>
        /// <param name="simpleNodeName"></param>
        /// <param name="simpleType"></param>
        /// <param name="complexNodeName"></param>
        /// <param name="complexType"></param>
        /// <param name="key"></param>
        /// <param name="root"></param>
        /// <param name="metadataRef"></param>
        /// <param name="mappingManager"></param>
        private static void createFromKeyMapping(
            string simpleNodeName, LinkElementType simpleType,
            string complexNodeName, LinkElementType complexType,
            Key key,
            Mapping root,
            XDocument metadataRef,
            MappingManager mappingManager)
        {
            LinkElement le = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(key),
                    key.ToString(), LinkElementType.Key, LinkElementComplexity.Simple);

            if (simpleNodeName.Equals(complexNodeName))
            {
                IEnumerable<XElement> elements = XmlUtility.GetXElementByNodeName(simpleNodeName, metadataRef);

                foreach (XElement xElement in elements)
                {
                    string sId = xElement.Attribute("id").Value;
                    string name = xElement.Attribute("name").Value;
                    LinkElement tmp = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(sId), name,
                        simpleType, LinkElementComplexity.Simple);

                    Mapping tmpMapping = MappingHelper.CreateIfNotExistMapping(le, tmp, 1, null, root);
                    MappingHelper.CreateIfNotExistMapping(le, tmp, 2, null, tmpMapping);
                }
            }
            else
            {
                IEnumerable<XElement> complexElements = XmlUtility.GetXElementByNodeName(complexNodeName, metadataRef);

                foreach (var complex in complexElements)
                {
                    string sIdComplex = complex.Attribute("id").Value;
                    string nameComplex = complex.Attribute("name").Value;
                    LinkElement tmpComplexElement = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(sIdComplex), nameComplex,
                        complexType, LinkElementComplexity.Complex);

                    Mapping complexMapping = MappingHelper.CreateIfNotExistMapping(le, tmpComplexElement, 1, null, root);


                    IEnumerable<XElement> simpleElements = XmlUtility.GetAllChildren(complex).Where(s => s.Name.LocalName.Equals(simpleNodeName));

                    foreach (XElement xElement in simpleElements)
                    {
                        string sId = xElement.Attribute("id").Value;
                        string name = xElement.Attribute("name").Value;
                        LinkElement tmp = createLinkELementIfNotExist(mappingManager, Convert.ToInt64(sId), name,
                            simpleType, LinkElementComplexity.Simple);

                        MappingHelper.CreateIfNotExistMapping(le, tmp, 2, null, complexMapping);
                    }
                }


            }
        }


        private static LinkElement createLinkELementIfNotExist(
            MappingManager mappingManager,
            long id,
            string name,
            LinkElementType type,
            LinkElementComplexity complexity)
        {
            LinkElement element = mappingManager.GetLinkElement(id, type);

            if (element == null)
            {
                element = mappingManager.CreateLinkElement(
                    id,
                    type,
                    complexity,
                    name,
                    ""
                    );
            }

            return element;
        }

        private static List<XElement> getXElements(string nodename, XDocument metadataRef)
        {
            if (!nodename.Contains("/"))
            {
                return XmlUtility.GetXElementByNodeName(nodename, metadataRef).ToList();
            }
            else
            {
                List<XElement> tmp = new List<XElement>();
                tmp.Add(metadataRef.XPathSelectElement(nodename));
                return tmp;
            }
        }

        private static void ImportPartyTypes()
        {
            //PartyTypeManager partyTypeManager = new PartyTypeManager();
            //var filePath = Path.Combine(AppConfiguration.GetModuleWorkspacePath("BAM"), "partyTypes.xml");
            //XDocument xDoc = XDocument.Load(filePath);
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.Load(xDoc.CreateReader());
            //var partyTypesNodeList = xmlDoc.SelectNodes("//PartyTypes");
            //if (partyTypesNodeList.Count > 0)
            //    foreach (XmlNode partyTypeNode in partyTypesNodeList[0].ChildNodes)
            //    {
            //        var title = partyTypeNode.Attributes["Name"].Value;
            //        //If there is not such a party type
            //        if (partyTypeManager.Repo.Get(item => item.Title == title).Count == 0)
            //        {
            //            //
            //            var partyType = partyTypeManager.Create(title, "Imported from partyTypes.xml", null);
            //            partyTypeManager.AddStatusType(partyType, "Create", "", 0);
            //            foreach (XmlNode customAttrNode in partyTypeNode.ChildNodes)
            //            {
            //                var customAttrType = customAttrNode.Attributes["type"] == null ? "String" : customAttrNode.Attributes["type"].Value;
            //                var description = customAttrNode.Attributes["description"] == null ? "" : customAttrNode.Attributes["description"].Value;
            //                var validValues = customAttrNode.Attributes["validValues"] == null ? "" : customAttrNode.Attributes["validValues"].Value;
            //                var isValueOptional = customAttrNode.Attributes["isValueOptional"] == null ? true : Convert.ToBoolean(customAttrNode.Attributes["isValueOptional"].Value);
            //                partyTypeManager.CreatePartyCustomAttribute(partyType, customAttrType, customAttrNode.Attributes["Name"].Value, description, validValues, isValueOptional);
            //            }
            //        }
            //        //edit add other custom attr

            //    }

        }
    }
}