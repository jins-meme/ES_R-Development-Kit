
package com.jins_jp.meme.academic.graph;

import android.app.Activity;
import android.content.Context;
import android.graphics.Color;
import android.graphics.Paint;
import android.os.Handler;
import android.widget.LinearLayout;

import org.achartengine.ChartFactory;
import org.achartengine.GraphicalView;
import org.achartengine.chart.BarChart;
import org.achartengine.chart.PointStyle;
import org.achartengine.model.XYMultipleSeriesDataset;
import org.achartengine.model.XYSeries;
import org.achartengine.renderer.XYMultipleSeriesRenderer;
import org.achartengine.renderer.XYSeriesRenderer;

import java.util.ArrayList;
import java.util.List;

import com.jins_jp.meme.academic.R;

public class GraphEOG {

    Context context;
    Activity activity;
    Handler handler;

    private GraphicalView graphicalView;
    private  LinearLayout chartlayout;

    private XYMultipleSeriesDataset dataset;

    private final int graph_width = 50;

    public GraphEOG(Context context, Activity activity, Handler handler) {
        this.context = context;
        this.activity = activity;
        this.handler = handler;
    }

    private XYMultipleSeriesDataset buildDataset(String[] titles,
                                                 List<double[]> xValues, List<double[]> yValues) {
        XYMultipleSeriesDataset dataset = new XYMultipleSeriesDataset();
        addXYSeries(dataset, titles, xValues, yValues, 0);
        return dataset;
    }

    private void addXYSeries(XYMultipleSeriesDataset dataset, String[] titles,
                             List<double[]> xValues, List<double[]> yValues, int scale) {
        int length = titles.length;
        for (int i = 0; i < length; i++) {
            XYSeries series = new XYSeries(titles[i], scale);
            double[] xV = xValues.get(i);
            double[] yV = yValues.get(i);
            int seriesLength = xV.length;
            for (int k = 0; k < seriesLength; k++) {
                series.add(xV[k], yV[k]);
            }
            dataset.addSeries(series);
        }
    }

    private XYMultipleSeriesRenderer buildRenderer(int[] colors,
                                                   PointStyle[] styles) {
        XYMultipleSeriesRenderer renderer = new XYMultipleSeriesRenderer();
        setRenderer(renderer, colors, styles);
        return renderer;
    }

    private void setRenderer(XYMultipleSeriesRenderer renderer, int[] colors,
                             PointStyle[] styles) {
        renderer.setChartTitleTextSize(30);
        renderer.setAxisTitleTextSize(15);
        renderer.setLabelsTextSize(15);
        renderer.setLegendTextSize(20);
        renderer.setPointSize(0f);
        renderer.setMargins(new int[] { 5, 5, 5, 5 });
        int length = colors.length;
        for (int i = 0; i < length; i++) {
            XYSeriesRenderer r = new XYSeriesRenderer();
            r.setColor(colors[i]);
            r.setPointStyle(styles[i]);
            renderer.addSeriesRenderer(r);
        }
    }

    private void setChartSettings(XYMultipleSeriesRenderer renderer,
                                  String title, String xTitle, String yTitle, double xMin,
                                  double xMax, double yMin, double yMax, int axesColor,
                                  int labelsColor) {
        renderer.setChartTitle(title);
        renderer.setXTitle(xTitle);
        renderer.setYTitle(yTitle);
        renderer.setXAxisMin(xMin);
        renderer.setXAxisMax(xMax);
        renderer.setYAxisMin(yMin);
        renderer.setYAxisMax(yMax);
        renderer.setAxesColor(axesColor);
        renderer.setLabelsColor(labelsColor);
    }

    public void makeChart(){
        chartlayout = (LinearLayout) activity.findViewById(R.id.graph_VvVh);
        graphicalView = graphMake();
        chartlayout.removeAllViews();
        chartlayout.addView(graphicalView);
    }

    private GraphicalView graphMake(){

        String[] titles = new String[] { "Vv", "Vh" };
        List<double[]> x = new ArrayList<double[]>();
        List<double[]> y = new ArrayList<double[]>();
        for (int i = 0; i < titles.length; i++) {
            x.add(new double[] { 0 });
            y.add(new double[]{ 0 });
        }

        int[] colors = new int[] { Color.BLUE, Color.RED };
        PointStyle[] styles = new PointStyle[] { PointStyle.CIRCLE, PointStyle.DIAMOND };

        XYMultipleSeriesRenderer renderer = buildRenderer(colors, styles);

        int length = renderer.getSeriesRendererCount();

        for (int i = 0; i < length; i++) {
            ((XYSeriesRenderer) renderer.getSeriesRendererAt(i)).setFillPoints(true);
        }

        setChartSettings(renderer, "EOG", "Time",
                "", 0, graph_width, -400, 400, Color.LTGRAY, Color.LTGRAY);

        renderer.setXLabels(10);
        renderer.setYLabels(10);
        renderer.setShowGrid(true);
        renderer.setXLabelsAlign(Paint.Align.RIGHT);
        renderer.setYLabelsAlign(Paint.Align.RIGHT);
        renderer.setZoomButtonsVisible(true);
        renderer.setPanEnabled(false, false);
        renderer.setZoomEnabled(false, false);

        dataset = buildDataset(titles, x, y);

        // Line curve
        GraphicalView gView = ChartFactory.getLineChartView(
                activity.getApplicationContext(), dataset, renderer);

        LinearLayout cHartEngineLayout = (LinearLayout) activity.findViewById(R.id.graph_VvVh);
        cHartEngineLayout.addView(gView);
        return gView;
    }

    public void setGraphEOG(int cnt, short Vv, short Vh) {
        if ((cnt % graph_width) == 0) {
            dataset.getSeriesAt(0).clear();
            dataset.getSeriesAt(1).clear();
        }
        dataset.getSeriesAt(0).add((cnt % graph_width), Vv);
        dataset.getSeriesAt(1).add((cnt % graph_width), Vh);
        graphicalView.repaint();
    }
}
