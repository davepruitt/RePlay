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
    public class RePlay_Game_VNS_Chart
    {
        #region Private data members

        private List<double> yvals = new List<double>();

        private PlotView plot_view;
        private PlotModel plot_model;

        private double positive_noise_threshold = 0;
        private double negative_noise_threshold = 0;

        #endregion

        #region Constructor

        public RePlay_Game_VNS_Chart(PlotView pv)
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

            //Add line annotations to the model for VNS purposes
            LineAnnotation positive_threshold_annotation = new LineAnnotation()
            {
                Y = 0,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 2,
                Color = OxyColor.FromRgb(105, 190, 40),
                Type = LineAnnotationType.Horizontal,
                Tag = "pt"
            };

            LineAnnotation negative_threshold_annotation = new LineAnnotation()
            {
                Y = 0,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 2,
                Color = OxyColor.FromRgb(105, 190, 40),
                Type = LineAnnotationType.Horizontal,
                Tag = "nt"
            };

            LineAnnotation positive_noise_thresh_annotation = new LineAnnotation()
            {
                Y = 0,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromRgb(255, 0, 0),
                Type = LineAnnotationType.Horizontal,
                Tag = "pnt"
            };

            LineAnnotation negative_noise_thresh_annotation = new LineAnnotation()
            {
                Y = 0,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1,
                Color = OxyColor.FromRgb(255, 0, 0),
                Type = LineAnnotationType.Horizontal,
                Tag = "nnt"
            };

            //Assign the line annotations to the model
            plot_model.Annotations.Add(positive_threshold_annotation);
            plot_model.Annotations.Add(negative_threshold_annotation);
            plot_model.Annotations.Add(positive_noise_thresh_annotation);
            plot_model.Annotations.Add(negative_noise_thresh_annotation);

            //Assign the model to the view
            plot_view.Model = plot_model;
            plot_view.Invalidate();
        }

        #endregion

        #region Methods

        public void SetYAxisLabel(string y_axis_label)
        {
            var y_axis = plot_model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();
            if (y_axis != null)
            {
                y_axis.Title = y_axis_label;
            }
        }

        public void SetYAxisLimits(double min, double max)
        {
            var y_axis = plot_model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();
            if (y_axis != null)
            {
                y_axis.Minimum = min;
                y_axis.Maximum = max;
            }
        }

        public void SetNoiseThresholds(double pnt, double nnt)
        {
            var pnt_annotation = plot_model.Annotations.Where(x => (x.Tag as string).Equals("pnt")).FirstOrDefault() as LineAnnotation;
            if (pnt_annotation != null)
            {
                pnt_annotation.Y = pnt;
                positive_noise_threshold = pnt;
            }

            var nnt_annotation = plot_model.Annotations.Where(x => (x.Tag as string).Equals("nnt")).FirstOrDefault() as LineAnnotation;
            if (nnt_annotation != null)
            {
                nnt_annotation.Y = nnt;
                negative_noise_threshold = nnt;
            }
        }

        public void AddDataPoint(double y_val, double pt, double nt)
        {
            //Get the y-axis
            var y_axis = plot_model.Axes.Where(x => x.Position == AxisPosition.Left).FirstOrDefault();

            //Add a new datapoint to the buffer
            yvals.Add(y_val);
            if (yvals.Count > 60)
            {
                yvals.RemoveAt(0);
            }

            //Plot the data
            var line_series = plot_model.Series.FirstOrDefault() as LineSeries;
            if (line_series != null)
            {
                line_series.Points.Clear();
                line_series.Points.AddRange(yvals.Select((y, x) => new DataPoint(x, y)).ToList());
            }

            //Adjust the threshold annotations
            var pt_annotation = plot_model.Annotations.Where(x => (x.Tag as string).Equals("pt")).FirstOrDefault() as LineAnnotation;
            if (pt_annotation != null)
            {
                pt_annotation.Y = pt;
            }

            var nt_annotation = plot_model.Annotations.Where(x => (x.Tag as string).Equals("nt")).FirstOrDefault() as LineAnnotation;
            if (nt_annotation != null)
            {
                nt_annotation.Y = nt;
            }

            //Adjust the y-axis bounds
            if (y_axis != null)
            {
                double max_val = Math.Max(yvals.Max(), Math.Max(pt, positive_noise_threshold));
                double min_val = Math.Min(yvals.Min(), Math.Min(nt, negative_noise_threshold));

                if (double.IsNaN(y_axis.Maximum) || y_axis.Maximum < max_val)
                {
                    y_axis.Maximum = max_val * 1.25;
                }

                if (double.IsNaN(y_axis.Minimum) || y_axis.Minimum > min_val)
                {
                    y_axis.Minimum = min_val * 1.25;
                }
            }

            //Invalidate the plot
            plot_model.InvalidatePlot(true);
        }

        #endregion
    }
}