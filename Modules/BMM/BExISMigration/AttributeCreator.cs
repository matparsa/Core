﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Entities.DataStructure;

namespace BExISMigration
{
    public class AttributeCreator
    {
        // create read attributes in bpp
        public void CreateAttributes(ref DataTable mappedAttributes)
        {
            foreach (DataRow mapAttributesRow in mappedAttributes.Rows)
            {
                DataContainerManager attributeManager = new DataContainerManager();
                DataTypeManager dataTypeManager = new DataTypeManager();
                UnitManager unitManager = new UnitManager();
                DataAttribute attribute = new DataAttribute();

                // values of the attribute
                attribute.ShortName = mapAttributesRow["ShortName"].ToString();
                attribute.Name = mapAttributesRow["Name"].ToString();
                attribute.Description = mapAttributesRow["Description"].ToString();
                attribute.IsMultiValue = false;
                attribute.IsBuiltIn = false;
                //attribute.Owner = "BMM";
                attribute.Scope = "";
                attribute.MeasurementScale = MeasurementScale.Categorial; ////////!!!!!!!!fromMapping??????????????????
                attribute.ContainerType = DataContainerType.ReferenceType;
                attribute.EntitySelectionPredicate = "";
                attribute.DataType = dataTypeManager.Repo.Get(Convert.ToInt64(mapAttributesRow["DataTypeId"]));
                attribute.Unit = unitManager.Repo.Get(Convert.ToInt64(mapAttributesRow["UnitId"]));
                attribute.Methodology = null;
                attribute.Classification = null;
                attribute.AggregateFunctions = null;
                attribute.GlobalizationInfos = null;
                attribute.Constraints = null;
                attribute.ExtendedProperties = null;

                DataAttribute dataAttribute = new DataAttribute();
                DataAttribute existAttribute = attributeManager.DataAttributeRepo.Get(a =>
                    attribute.Name.Equals(a.Name) &&
                    attribute.ShortName.Equals(a.ShortName) &&
                    attribute.Unit.Id.Equals(a.Unit.Id) &&
                    attribute.DataType.Id.Equals(a.DataType.Id)
                    ).FirstOrDefault();

                // if attribute not exists (name, shortName) then create
                if (existAttribute == null)
                {
                    dataAttribute = attributeManager.CreateDataAttribute(
                        attribute.ShortName,
                        attribute.Name,
                        attribute.Description,
                        attribute.IsMultiValue,
                        attribute.IsBuiltIn,
                        attribute.Scope,
                        attribute.MeasurementScale,
                        attribute.ContainerType,
                        attribute.EntitySelectionPredicate,
                        attribute.DataType,
                        attribute.Unit,
                        attribute.Methodology,
                        attribute.Classification,
                        attribute.AggregateFunctions,
                        attribute.GlobalizationInfos,
                        attribute.Constraints,
                        attribute.ExtendedProperties
                        );
                }
                else
                {
                    dataAttribute = existAttribute;
                }

                // add attributeId to the mappedAttributes Table
                mapAttributesRow["AttributeId"] = dataAttribute.Id;
            }
        }


        // create read units in bpp
        public void CreateUnits(ref DataTable mappedUnits, DataTable mappedDataTypes)
        {
            foreach (DataRow mapUnitsRow in mappedUnits.Rows)
            {
                UnitManager unitManager = new UnitManager();
                DataTypeManager dataTypeManger = new DataTypeManager();
                Unit unit = new Unit();

                // values of the unit
                unit.Name = mapUnitsRow["Name"].ToString();
                unit.Abbreviation = mapUnitsRow["Abbreviation"].ToString();
                unit.Description = mapUnitsRow["Description"].ToString();

                unit.Dimension = unitManager.DimensionRepo.Get(Convert.ToInt64(mapUnitsRow["DimensionId"]));

                // find measurement system
                foreach (MeasurementSystem msCheck in Enum.GetValues(typeof(MeasurementSystem)))
                {
                    if (msCheck.ToString().Equals(mapUnitsRow["MeasurementSystem"].ToString()))
                    {
                        unit.MeasurementSystem = msCheck;
                    }
                }

                // get existing unit or create
                Unit existU = unitManager.Repo.Get(u =>
                    unit.Abbreviation.Equals(u.Abbreviation) &&
                    unit.Dimension.Id.Equals(u.Dimension.Id)
                    ).FirstOrDefault();
                if (existU == null)
                    unit = unitManager.Create(unit.Name, unit.Abbreviation, unit.Description, unit.Dimension, unit.MeasurementSystem);
                else
                    unit = existU;

                // set data type to created unit or add data type to existing unit
                List<string> Types = mapUnitsRow["DataTypes"].ToString().Split(' ').ToList<string>();

                List<DataType> newDataTypes = new List<DataType>();
                foreach (string type in Types)
                {
                    DataRow row = mappedDataTypes.Select("Name = '" + type + "'").First<DataRow>();
                    newDataTypes.Add(dataTypeManger.Repo.Get(Convert.ToInt64(row["DataTypesId"])));
                }

                // add bpp-dataTypes to the unit
                List<DataType> existingDataTypes = unit.AssociatedDataTypes.ToList();
                List<DataType> tobeAddedDataTypes = newDataTypes.Except(existingDataTypes).ToList();
                if (tobeAddedDataTypes != null && tobeAddedDataTypes.Count() > 0)
                {
                    foreach (DataType dt in tobeAddedDataTypes)
                    {
                        if (!(unit.AssociatedDataTypes.Contains(dt)))
                            unitManager.AddAssociatedDataType(unit, dt);
                    }
                }

                // add unit-ID to the mappedUnits Table
                mapUnitsRow["UnitId"] = unit.Id;
            }
        }


        // create dimensions in bpp
        public void CreateDimensions(ref DataTable mappedDimensions)
        {
            UnitManager unitManager = new UnitManager();

            foreach (DataRow mappedDimension in mappedDimensions.Rows)
            {
                string dimensionName = mappedDimension["Name"].ToString();
                string dimensionDescription = mappedDimension["Description"].ToString();
                string dimensionSpecification = mappedDimension["Syntax"].ToString();

                Dimension dimension = unitManager.DimensionRepo.Get().Where(d => d.Name.Equals(dimensionName)).FirstOrDefault();

                if (dimension == null)
                {
                    dimension = unitManager.Create(dimensionName, dimensionDescription, dimensionSpecification);
                }

                mappedDimension["DimensionId"] = dimension.Id;
            }
        }


        // create read data types in bpp
        public void CreateDataTypes(ref DataTable mappedDataTypes)
        {
            DataTypeManager dataTypeManager = new DataTypeManager();

            foreach (DataRow mappedDataType in mappedDataTypes.Rows)
            {
                string dtName = mappedDataType["Name"].ToString();
                string dtDescription = mappedDataType["Description"].ToString();
                TypeCode dtSystemType = new TypeCode();
                foreach (TypeCode type in Enum.GetValues(typeof(TypeCode)))
                {
                    if (type.ToString().Equals(mappedDataType["SystemType"].ToString()))
                    {
                        dtSystemType = type;
                    }
                }

                DataType dataType = new DataType();
                // get existing dataTypes
                DataType existDT = dataTypeManager.Repo.Get().Where(d =>
                    d.Name.Equals(dtName) &&
                    d.SystemType.ToString().Equals(mappedDataType["SystemType"].ToString())
                    ).FirstOrDefault();
                // return ID of existing dataType or create dataType
                if (existDT == null && dtSystemType != null)
                {
                    dataType = dataTypeManager.Create(dtName, dtDescription, dtSystemType);
                }
                else
                {
                    dataType = existDT;
                }

                mappedDataType["DataTypesId"] = dataType.Id;
            }
        }
    }
}
