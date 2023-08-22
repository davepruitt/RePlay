using System;
using System.Collections.Generic;

namespace RePlay.Manager
{
    /// <summary>
    /// A generic class for returning a list of items to display on the current page
    /// </summary>
    public class Paginator<T>
    {
        #region Properties

        protected List<T> ItemsList { get; set; }

        public int ItemsPerPage { get; private set; }

        public int TotalNumItems { get; set; }

        public int ItemsRemaining  { get; set; }

        public int LastPage { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor. Creates a paginator object from a specified list of 
        /// items and a number of items that should be shown on each page.
        /// </summary>
        public Paginator(int itemsPerPage, List<T> itemsList)
        {
            ItemsList = itemsList;
            TotalNumItems = ItemsList.Count;
            ItemsPerPage = itemsPerPage;
            ItemsRemaining = TotalNumItems % ItemsPerPage;
            LastPage = Math.Max((TotalNumItems - 1) / ItemsPerPage, 0);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates the current page of items with ItemsPerPage items on each page
        /// </summary>
        public List<T> GeneratePage(int curr)
        {
            int start = curr * ItemsPerPage;

            List<T> data = new List<T>();

            for (int i = start; i < Math.Min(start + ItemsPerPage, ItemsList.Count); i++)
            {
                data.Add(ItemsList[i]);
            }

            return data;
        }

        /// <summary>
        /// Removes an item from the specified position in the list
        /// </summary>
        public T RemoveAt(int position)
        {
            T item = ItemsList[position];
            ItemsList.RemoveAt(position);
            TotalNumItems = ItemsList.Count;
            ItemsRemaining = TotalNumItems % ItemsPerPage;
            LastPage = Math.Max((TotalNumItems - 1) / ItemsPerPage, 0);

            return item;
        }

        /// <summary>
        /// This method returns whether we are currently on the last page
        /// </summary>
        public bool ContainsLast(int curr)
        {
            return (curr == LastPage);
        }

        #endregion
    }
}
