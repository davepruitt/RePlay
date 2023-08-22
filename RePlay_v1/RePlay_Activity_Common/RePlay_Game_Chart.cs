using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Xamarin.Android;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RePlay_Activity_Common
{
    public class RePlay_Game_Chart
    {
        #region Private data members

        private List<double> yvals = new List<double>();
        private List<double> yvals_abs = new List<double>();
        private double yvals_abs_max = 0;

        private bool ymin_setbyuser = false;
        private bool ymax_setbyuser = false;

        private double max_vals_average = 0;
        private List<double> max_vals = new List<double>();
        private DateTime last_check = DateTime.MinValue;
        private TimeSpan check_timespan = TimeSpan.FromSeconds(1.0);

        private PlotView plot_view;
        private PlotModel plot_model;

        #endregion

        #region Constructor

        public RePlay_Game_Chart (PlotView pv)
        {
            //Set the chart view
            plot_view = pv;

            //Create a model for the plot
            plot_model = new PlotModel();

            //Create axes
            LinearAxis y_axis = new LinearAxis()
            {
                Position = AxisPosition.Left,
                Title = string.Empty,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MinorTickSize = 0
            };

            LinearAxis x_axis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MinorTickSize = 0,
                MajorStep = 100,
                TickStyle = TickStyle.None,
                TextColor = OxyColors.Transparent
            };

            //Assign the axes to the model
            plot_model.Axes.Add(x_axis);
            plot_model.Axes.Add(y_axis);

            //Create a line series to hold data
            LineSeries line_series = new LineSeries()
            {
                Color = OxyColors.Blue,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2.0
            };

            //Add the series to the plot model
            plot_model.Series.Add(line_series);

            //Assign the model to the view
            plot_view.Model = plot_model;
            plot_view.Invalidate();
        }

        #endregion

        #region Methods

        public void SetYAxisLabel (string y_axis_label)
        {
            var y_axis = plot_model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();
            if (y_axis != null)
            {
                y_axis.Title = y_axis_label;
            }
        }

        public void SetYAxisLimits (double min, double max)
        {
            var y_axis = plot_model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();
            if (y_axis != null)
            {
                if (!double.IsNaN(min))
                {
                    ymin_setbyuser = true;
                }

                if (!double.IsNaN(max))
                {
                    ymax_setbyuser = true;
                }

                y_axis.Minimum = min;
                y_axis.Maximum = max;
            }
        }

        public void AddHorizontalLineAnnotation (double yval, OxyColor color, LineStyle line_style = LineStyle.Dash)
        {
            LineAnnotation horizontal_line_annotation = new LineAnnotation()
            {
                Y = yval,
                Color = color,
                Type = LineAnnotationType.Horizontal,
                LineStyle = line_style
            };

            plot_model.Annotations.Add(horizontal_line_annotation);
        }

        public void AddDataPoint (double y_val)
        {
            //Add a new datapoint to the buffer
            yvals.Add(y_val);
            yvals_abs.Add(Math.Abs(y_val));
            if (yvals.Count > 60)
            {
                yvals.RemoveAt(0);
                yvals_abs.RemoveAt(0);
            }

            yvals_abs_max = yvals_abs.Max();

            //Maintain a list of the "max" value obtained approximately every second of gameplay
            if (DateTime.Now >= (last_check + check_timespan))
            {
                last_check = DateTime.Now;
                max_vals.Add(yvals_abs_max);
                if (max_vals.Count > 10)
                {
                    max_vals.RemoveAt(0);
                }

                max_vals_average = max_vals.Average();
            }

            //Plot the data
            var line_series = plot_model.Series.FirstOrDefault() as LineSeries;
            if (line_series != null)
            {
                line_series.Points.Clear();
                line_series.Points.AddRange(yvals.Select((y, x) => new DataPoint(x, y)).ToList());
            }

            //Adjust the y-axis limits
            var yaxis = plot_model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault() as LinearAxis;
            if (yaxis != null)
            {
                //Calculate the ylim based off of the y-values in the signal in the signal history
                var ylim = Math.Max(max_vals_average, yvals_abs_max);

                //Get the y-vals of all annotations and factor those into the ylim
                if (plot_model.Annotations.Count > 0)
                {
                    var max_y_annotation = plot_model.Annotations.Select(x => Math.Abs((x as LineAnnotation).Y)).Max();
                    ylim = Math.Max(ylim, max_y_annotation);
                }
                
                //Set the y-axis minimum and maximum
                if (!ymin_setbyuser)
                {
                    yaxis.Minimum = -ylim;
                }
                
                if (!ymax_setbyuser)
                {
                    yaxis.Maximum = ylim;
                }
            }

            //Invalidate the plot
            plot_model.InvalidatePlot(true);
        }

        #endregion
    }
}