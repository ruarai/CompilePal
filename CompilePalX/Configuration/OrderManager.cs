using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using CompilePalX.Compilers;

namespace CompilePalX.Configuration
{
	static class OrderManager
	{
		public static ObservableCollection<CompileProcess> CurrentOrder;
		private static object lockObj = new object();

		public static void Init()
		{
			CurrentOrder = new ObservableCollection<CompileProcess>();
			BindingOperations.EnableCollectionSynchronization(CurrentOrder, lockObj);
			CurrentOrder.CollectionChanged += CurrentOrderOnCollectionChanged;
		}

		private static void CurrentOrderOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			if (notifyCollectionChangedEventArgs.NewItems == null)
				return;

			//This gets called when a custom process is moved on the order grid
			foreach (var newItem in notifyCollectionChangedEventArgs.NewItems)
			{
				if (newItem is CustomProgram custProg)
				{
					//Get custom process
					var customProcess = (CustomProcess) ConfigurationManager.CompileProcesses
						.First(c => c.Metadata.DoRun
						            && c.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset)
						            && c.Name == "CUSTOM"
						);

					var customProgram = customProcess.Programs
						.First(c => c.Equals(custProg));

					//Update the paramater of the config item correlating to the program with the new order
					if (customProgram != null)
						MainWindow.GetMainWindow.UpdateItemOrder(ref customProgram, notifyCollectionChangedEventArgs.NewStartingIndex);
				}
			}

			if (notifyCollectionChangedEventArgs.OldItems != null )
				foreach (var oldItem in notifyCollectionChangedEventArgs.OldItems)
				{
					Console.WriteLine("old: " + oldItem);
				}
		}

		public static void UpdateOrder()
		{
			//Get all default processes for config
			var defaultProcs = new List<CompileProcess>(ConfigurationManager.CompileProcesses
				.Where(c => c.Metadata.DoRun
					        && c.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset)
					        && c.Name != "ORDER"
					        && c.Name != "CUSTOM"
				).ToList());

			//Get custom process
			var customProcess = (CustomProcess) ConfigurationManager.CompileProcesses
				.FirstOrDefault(c => c.Metadata.DoRun
					        && c.PresetDictionary.ContainsKey(ConfigurationManager.CurrentPreset)
					        && c.Name == "CUSTOM"
				);

			var newOrder = new ObservableCollection<CompileProcess>(defaultProcs);

			if (customProcess != null)
			{
				var orderedCustomPrograms = customProcess.BuildProgramList().OrderBy(c => c.CustomOrder).ToList();

				foreach (var program in orderedCustomPrograms)
				{
					if (program.CustomOrder > defaultProcs.Count)
					{
						newOrder.Add(program);
					}
					else
					{
						newOrder.Insert(program.CustomOrder, program);
					}
				}
			}

			//Update order
			CurrentOrder.Clear();
			CurrentOrder.AddRange(newOrder);

			MainWindow.GetMainWindow.UpdateOrderGridSource(CurrentOrder);
		}
	}
}
