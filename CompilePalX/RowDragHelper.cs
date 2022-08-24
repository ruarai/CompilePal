using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CompilePalX
{
    //Adapated from http://www.eidias.com/blog/2014/8/15/movable-rows-in-wpf-datagrid
    public static class RowDragHelper
    {
        public static readonly DependencyProperty EnableRowDragProperty =
            DependencyProperty.RegisterAttached("EnableRowDrag", typeof(bool), typeof(RowDragHelper),
                new PropertyMetadata(false, EnableRowDragChanged));

        private static readonly DependencyProperty DraggedItemProperty =
            DependencyProperty.RegisterAttached("DraggedItem", typeof(object), typeof(RowDragHelper),
                new PropertyMetadata(null));

        public static bool GetEnableRowDrag(DataGrid obj)
        {
            return (bool)obj.GetValue(EnableRowDragProperty);
        }

        public static void SetEnableRowDrag(DataGrid obj, bool value)
        {
            obj.SetValue(EnableRowDragProperty, value);
        }

        private static object GetDraggedItem(DependencyObject obj)
        {
            return obj.GetValue(DraggedItemProperty);
        }

        private static void SetDraggedItem(DependencyObject obj, object value)
        {
            obj.SetValue(DraggedItemProperty, value);
        }

        private static void EnableRowDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DataGrid grid))
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                grid.PreviewMouseLeftButtonDown += RowDragOnPreviewMouseLeftButtonDown;
                grid.PreviewMouseLeftButtonUp += RowDragOnPreviewMouseLeftButtonUp;
                grid.MouseMove += RowDragOnMouseMove;
            }
            else
            {
                grid.PreviewMouseLeftButtonDown -= RowDragOnPreviewMouseLeftButtonDown;
                grid.PreviewMouseLeftButtonUp -= RowDragOnPreviewMouseLeftButtonUp;
                grid.MouseMove -= RowDragOnMouseMove;
            }
        }

        private static void RowDragOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var draggedRow = GetDraggedItem(sender as DependencyObject);
            var g = sender as DataGrid;

            if (draggedRow == null || g == null)
            {
                return;
            }

            var targetRow = GetRowFromPoint(g, mouseEventArgs.GetPosition(g));

            if (targetRow == null)
            {
                return;
            }

            ExchangeRows(g, targetRow.Item);
        }

        private static void RowDragOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var draggedItem = GetDraggedItem(sender as DependencyObject);
            if (draggedItem == null)
            {
                return;
            }

            //disabled because it seems to glitch out when 2 custom programs are swapped
            //ExchangeRows(sender, ((DataGrid) sender).SelectedItem);

            ((DataGrid)sender).SelectedItem = draggedItem;
            SetDraggedItem(sender as DataGrid, null);
        }

        private static void RowDragOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            //Get row index from click
            //Adapted from https://social.msdn.microsoft.com/Forums/vstudio/en-US/22373fc7-c677-46a1-80ff-3e262836ecc1/right-click-in-wpf-datagrid?forum=wpf
            var g = sender as DataGrid;
            var p = mouseButtonEventArgs.GetPosition(g);

            if (g == null)
            {
                return;
            }

            var selectedRow = GetRowFromPoint(g, p);

            if (selectedRow == null)
            {
                return;
            }

            //Prevent user from reordering processes not marked as orderable
            var isDraggable = (selectedRow.Item as CompileProcess)?.IsDraggable;
            if (isDraggable != null && !(bool)isDraggable)
            {
                return;
            }

            SetDraggedItem((DataGrid)sender, selectedRow.Item);
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
            {
                return null;
            }

            if (parentObject is T parent)
            {
                return parent;
            }

            return FindVisualParent<T>(parentObject);
        }

        private static DataGridRow GetRowFromPoint(DataGrid g, Point p)
        {
            DataGridRow selectedRow = null;
            VisualTreeHelper.HitTest(g, null, result =>
                {
                    var row = FindVisualParent<DataGridRow>(result.VisualHit);

                    if (row != null)
                    {
                        selectedRow = row;
                        return HitTestResultBehavior.Stop;
                    }

                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(p));
            return selectedRow;
        }

        private static void ExchangeRows(object sender, object target)
        {
            var draggedRow = GetDraggedItem(sender as DependencyObject);

            if (draggedRow == null)
            {
                return;
            }

            if (target != null && !ReferenceEquals(draggedRow, target))
            {
                var list = ((DataGrid)sender).ItemsSource as IList;
                if (list == null)
                {
                    return;
                }

                var oldIndex = list.IndexOf(draggedRow);

                var targetIndex = list.IndexOf(target);
                list.Remove(draggedRow);
                list.Insert(targetIndex, draggedRow);

                //Console.WriteLine("Row Switch");

                var args = new RowSwitchEventArgs
                {
                    PrimaryRowIndex = targetIndex, DisplacedRowIndex = oldIndex
                };

                OnRowSwitch(args);
            }

        }

        public static void OnRowSwitch(RowSwitchEventArgs e)
        {
            var handler = RowSwitched;
            handler?.Invoke(typeof(RowDragHelper), e);
        }

        public static event EventHandler<RowSwitchEventArgs> RowSwitched;
    }

    public class RowSwitchEventArgs : EventArgs
    {
        public int DisplacedRowIndex; //Row of item being displaced by row swap
        public int PrimaryRowIndex; //Row of the item being swapped
    }
}
