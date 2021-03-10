﻿using LibDotNetParser.DotNet.Tabels.Defs;
using LibDotNetParser.PE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibDotNetParser.DotNet.Tabels
{
    public class Tabels
    {
        private MetadataReader r;
        public List<Module> ModuleTabel { get; }
        public List<TypeRef> TypeRefTabel { get; }
        public List<TypeDef> TypeDefTabel { get; }
        public List<FieldTabelRow> FieldTabel { get; }
        public List<Method> MethodTabel { get; }
        public List<Param> ParmTabel { get; }
        public List<InterfaceImpl> InterfaceImplTable { get; }
        public List<MemberRef> MemberRefTabelRow { get; }
        public List<Constant> ConstantTabel { get; }
        public List<CustomAttribute> CustomAttributeTabel { get; }
        public List<FieldMarshal> FieldMarshalTabel { get; }
        public List<DeclSecurity> DeclSecurityTabel { get; }
        public List<ClassLayout> ClassLayoutTabel { get; }
        public List<FieldLayout> FieldLayoutTabel { get; }
        public List<StandAloneSig> StandAloneSigTabel { get; }
        public List<EventMap> EventMapTabel { get; }
        public List<Event> EventTabel { get; }
        public List<PropertyMap> PropertyMapTabel { get; }
        public List<PropertyTabel> PropertyTabel { get; }
        public List<MethodSemantics> MethodSemanticsTabel { get; }
        public List<MethodImpl> MethodImplTabel { get; }
        public List<ModuleRef> ModuleRefTabel { get; }
        public List<TypeSpec> TypeSpecTabel { get; }
        public List<ImplMap> ImplMapTabel { get; }
        public List<FieldRVA> FieldRVATabel { get; }
        public List<Assembly> AssemblyTabel { get; }
        public Tabels(PEFile p)
        {
            //Init
            this.r = p.MetadataReader;

            //Read all of the tabels
            ModuleTabel = new List<Module>();
            TypeRefTabel = new List<TypeRef>();
            TypeDefTabel = new List<TypeDef>();
            FieldTabel = new List<FieldTabelRow>();
            MethodTabel = new List<Method>();
            ParmTabel = new List<Param>();
            InterfaceImplTable = new List<InterfaceImpl>();
            MemberRefTabelRow = new List<MemberRef>();
            ConstantTabel = new List<Constant>();
            CustomAttributeTabel = new List<CustomAttribute>();
            FieldMarshalTabel = new List<FieldMarshal>();
            DeclSecurityTabel = new List<DeclSecurity>();
            ClassLayoutTabel = new List<ClassLayout>();
            FieldLayoutTabel = new List<FieldLayout>();
            StandAloneSigTabel = new List<StandAloneSig>();
            EventMapTabel = new List<EventMap>();
            EventTabel = new List<Event>();
            PropertyMapTabel = new List<PropertyMap>();
            PropertyTabel = new List<PropertyTabel>();
            MethodSemanticsTabel = new List<MethodSemantics>();
            MethodImplTabel = new List<MethodImpl>();
            ModuleRefTabel = new List<ModuleRef>();
            TypeSpecTabel = new List<TypeSpec>();
            ImplMapTabel = new List<ImplMap>();
            FieldRVATabel = new List<FieldRVA>();
            AssemblyTabel = new List<Assembly>();

            int a = 0;
            //Read module Tabel (if any)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Module) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new Module();
                    m.Read(r);
                    ModuleTabel.Add(m);
                }
                a++;
            }
            //Read TypeRef Tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.TypeRef) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new TypeRef();
                    m.Read(r);
                    TypeRefTabel.Add(m);
                }
                a++;
            }
            //Read TypeDef Tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.TypeDef) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new TypeDef();
                    m.Read(r);
                    TypeDefTabel.Add(m);
                }
                a++;
            }
            //Read Field Tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Field) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new FieldTabelRow();
                    m.Read(r);
                    FieldTabel.Add(m);
                }
                a++;
            }
            //Read Method tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Method) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new Method();
                    m.Read(r);
                    MethodTabel.Add(m);
                }
                a++;
            }
            //Read Parm Tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Param) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new Param();
                    m.Read(r);
                    ParmTabel.Add(m);
                }
                a++;
            }
            //Read interfaceimpl Tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.InterfaceImpl) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new InterfaceImpl();
                    m.Read(r);
                    InterfaceImplTable.Add(m);
                }
                a++;
            }
            //Read MemberRef tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.MemberRef) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new MemberRef();
                    m.Read(r);
                    MemberRefTabelRow.Add(m);
                }
                a++;
            }
            //Read Constant tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Constant) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new Constant();
                    m.Read(r);
                    ConstantTabel.Add(m);
                }
                a++;
            }
            //Read CustomAttribute tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.CustomAttribute) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new CustomAttribute();
                    m.Read(r);
                    CustomAttributeTabel.Add(m);
                }
                a++;
            }
            //Read FieldMarshal tabel (Please test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.FieldMarshal) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new FieldMarshal();
                    m.Read(r);
                    FieldMarshalTabel.Add(m);
                }
                a++;
            }
            //Read DeclSecurity tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.DeclSecurity) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new DeclSecurity();
                    m.Read(r);
                    DeclSecurityTabel.Add(m);
                }
                a++;
            }
            //Read ClassLayout tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.ClassLayout) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new ClassLayout();
                    m.Read(r);
                    ClassLayoutTabel.Add(m);
                }
                a++;
            }
            //Read FieldLayout tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.FieldLayout) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new FieldLayout();
                    m.Read(r);
                    FieldLayoutTabel.Add(m);
                }
                a++;
            }
            //Read StandAloneSig tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.StandAloneSig) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new StandAloneSig();
                    m.Read(r);
                    StandAloneSigTabel.Add(m);
                }
                a++;
            }
            //Read EventMap tabel (please test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.EventMap) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new EventMap();
                    m.Read(r);
                    EventMapTabel.Add(m);
                }
                a++;
            }
            //Read event tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Event) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new Event();
                    m.Read(r);
                    EventTabel.Add(m);
                }
                a++;
            }
            //Read Property Map tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.PropertyMap) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new PropertyMap();
                    m.Read(r);
                    PropertyMapTabel.Add(m);
                }
                a++;
            }
            //Read Property tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Property) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new PropertyTabel();
                    m.Read(r);
                    PropertyTabel.Add(m);
                }
                a++;
            }
            //Read MethodSemantics  tabel
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.MethodSemantics) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new MethodSemantics();
                    m.Read(r);
                    MethodSemanticsTabel.Add(m);
                }
                a++;
            }
            //Read MethodImpl tabel (Please test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.MethodImpl) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new MethodImpl();
                    m.Read(r);
                    MethodImplTabel.Add(m);
                }
                a++;
            }
            //Read ModuleRef tabel (pls test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.ModuleRef) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new ModuleRef();
                    m.Read(r);
                    ModuleRefTabel.Add(m);
                }
                a++;
            }
            //Read TypeSpec tabel (pls test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.TypeSpec) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new TypeSpec();
                    m.Read(r);
                    TypeSpecTabel.Add(m);
                }
                a++;
            }
            //Read ImplMap tabel (pls test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.ImplMap) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new ImplMap();
                    m.Read(r);
                    ImplMapTabel.Add(m);
                }
                a++;
            }
            //Read FieldRVA  tabel (pls test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.FieldRVA) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new FieldRVA();
                    m.Read(r);
                    FieldRVATabel.Add(m);
                }
                a++;
            }
            //Read Assembly tabel (pls test)
            if ((p.ClrMetaDataStreamHeader.TablesFlags & MetadataTableFlags.Assembly) != 0)
            {
                for (int i = 0; i < p.ClrMetaDataStreamHeader.TableSizes[a]; i++)
                {
                    var m = new Assembly();
                    m.Read(r);
                    var x = p.ClrStringsStream.GetByOffset(m.Name);
                    AssemblyTabel.Add(m);
                }
                a++;
            }
        }
    }
}
