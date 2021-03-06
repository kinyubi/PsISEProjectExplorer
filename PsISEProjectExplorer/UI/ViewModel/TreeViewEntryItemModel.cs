﻿using PsISEProjectExplorer.Enums;
using PsISEProjectExplorer.Model.DocHierarchy.Nodes;
using PsISEProjectExplorer.Services;
using PsISEProjectExplorer.UI.Helpers;
using System;
using System.Collections.Generic;

namespace PsISEProjectExplorer.UI.ViewModel
{
    public class TreeViewEntryItemModel : BaseViewModel
    {
        public static object RootLockObject = new object();

        private INode documentHierarchyNode;

        private INode DocumentHierarchyNode { 
            get
            {
                return this.documentHierarchyNode;
            }
            set
            {
                this.documentHierarchyNode = value;
                this.OnPropertyChanged("IsExpanded");
            }
        }

        public INode Node
        {
            get
            {
                var node = this.DocumentHierarchyNode as ViewNode;
                if (node != null)
                {
                    return node.Node;
                }
                return this.DocumentHierarchyNode;
            }
        }

        public string Image
        {
            get
            {
                string fileName = this.NodeType.ToString().ToLowerInvariant();
                if (!this.Node.IsValid)
                {
                    fileName += "_invalid";
                }
                return String.Format("Resources/{0}.png", fileName);
            }
        }

        public string Name
        {
            get
            {
                return this.Node.Name;
            }
        }

        public string Path
        {
            get
            {
                return this.Node.Path;
            }
        }

        public string Tooltip
        {
            get
            {
                return this.Node.Metadata;
            }
        }

        public TreeViewEntryItemModel Parent { get; private set; }

        private TreeViewEntryItemModelState State { get; set; }

        public bool IsExpanded
        {
            get
            {
                return this.State.IsExpanded;
            }
            set
            {
                this.State.IsExpanded = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get
            {
                return this.State.IsSelected;
            }
            set
            {
                this.State.IsSelected = value;
                this.OnPropertyChanged();
            }
        }
        

        private bool isBeingEdited;

        public bool IsBeingEdited
        {
            get
            {
                return this.isBeingEdited;
            }
            set
            {
                this.isBeingEdited = value;
                this.OnPropertyChanged();
            }
        }

        private bool isBeingAdded;

        public bool IsBeingAdded
        {
            get
            {
                return this.isBeingAdded;
            }
            set
            {
                this.isBeingAdded = value;
                this.OnPropertyChanged();
            }
        }

        public NodeType NodeType
        {
            get
            {
                return this.Node.NodeType;
            }
        }

        public TreeViewEntryItemObservableSet Children { get; private set; }

        public TreeViewEntryItemModel(INode node, TreeViewEntryItemModel parent, bool isSelected)
        {
            if (node == null) 
            {
                throw new ArgumentNullException("node");
            }
            var lockObject = this.Parent == null ? RootLockObject : this.Parent;
            lock (lockObject)
            {
                this.State = new TreeViewEntryItemModelState(false, isSelected);
                this.DocumentHierarchyNode = node;
                this.Parent = parent;
                this.Children = new TreeViewEntryItemObservableSet();
                if (this.Parent != null)
                {
                    this.Parent.Children.Add(this);
                }
            }
        }

        public void Delete()
        {
            var lockObject = this.Parent == null ? RootLockObject : this.Parent;
            lock (lockObject)
            {
                if (this.Parent != null)
                {
                    this.Parent.Children.Remove(this);
                }
                this.DocumentHierarchyNode = null;
                this.Children = null;
                this.Parent = null;
            }
        }

        public void UpdateNode(INode node)
        {
            if (this.DocumentHierarchyNode != node)
            {
                this.DocumentHierarchyNode = node;
                this.RefreshNode();
            }
        }

        public void RefreshNode()
        {
            this.OnPropertyChanged(String.Empty);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || this.GetType() != obj.GetType() || !(obj is TreeViewEntryItemModel))
            {
                return false;
            }
            var item = (TreeViewEntryItemModel)obj;
            return (this.Node == item.Node);
        }

        public override int GetHashCode()
        {
            return (this.Node == null ? 0 : this.Node.GetHashCode());
        }

    }
}
