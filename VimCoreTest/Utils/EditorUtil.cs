﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.ComponentModel.Composition.Hosting;

namespace VimCoreTest.Utils
{
    public static class EditorUtil
    {
        #region Factory

        public sealed class Factory
        {
            [Import]
            public ITextBufferFactoryService textBufferFactory;

            [Import]
            public ITextEditorFactoryService textEditorFactory;

            public Factory() { }
        }

        #endregion

        [ThreadStatic]
        private static CompositionContainer m_container;
        [ThreadStatic]
        private static Factory m_factory;

        public static CompositionContainer Container
        {
            get
            {
                if (null == m_container)
                {
                    m_container = CreateContainer();
                }
                return m_container;
            }
        }

        public static Factory FactoryService
        {
            get
            {

                if (null == m_factory)
                {
                    m_factory = new Factory();
                    Container.ComposeParts(m_factory);
                }
                return m_factory;
            }
        }


        public static ITextBuffer CreateBuffer(params string[] lines)
        {
            var factory = FactoryService.textBufferFactory;
            var buffer = factory.CreateTextBuffer();
            if (lines.Length != 0)
            {
                var text = lines.Aggregate((x, y) => x + Environment.NewLine + y);
                buffer.Replace(new Span(0, 0), text);
            }

            return buffer;
        }

        public static IWpfTextView CreateView(params string[] lines)
        {
            var buffer = CreateBuffer(lines);
            var view = FactoryService.textEditorFactory.CreateTextView(buffer);
            return view;
        }

        public static CompositionContainer CreateContainer()
        {
            try
            {
                var root = Path.GetDirectoryName(typeof(EditorUtil).Assembly.Location);
                var list = new List<ComposablePartCatalog>();
                list.Add(new AssemblyCatalog(Path.Combine(root, "Microsoft.VisualStudio.Platform.VSEditor.dll")));

                // Must include this because several editor options are actually stored as exported information 
                // on this DLL.  Including most importantly, the tabsize information
                list.Add(new AssemblyCatalog(Path.Combine(root, "Microsoft.VisualStudio.Text.Logic.dll")));

                // Include this DLL to get several more EditorOptions including WordWrapStyle
                list.Add(new AssemblyCatalog(Path.Combine(root, "Microsoft.VisualStudio.Text.UI.dll")));

                // Include this DLL to get more EditorOptions values
                list.Add(new AssemblyCatalog(Path.Combine(root, "Microsoft.VisualStudio.Text.UI.Wpf.dll")));

                // There is no default IUndoHistoryRegistry provided so I need to provide it here just to 
                // satisfy the MEF import
                list.Add(new AssemblyCatalog(typeof(EditorUtil).Assembly));

                var catalog = new AggregateCatalog(list.ToArray());
                return new CompositionContainer(catalog);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}