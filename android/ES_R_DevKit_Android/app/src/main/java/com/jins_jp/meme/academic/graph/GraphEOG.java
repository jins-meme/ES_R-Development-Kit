
package com.jins_jp.meme.academic.graph;

import android.app.Activity;
import android.content.Context;
import android.graphics.Color;
import android.os.Handler;

import com.github.mikephil.charting.charts.LineChart;
import com.github.mikephil.charting.components.XAxis;
import com.github.mikephil.charting.components.YAxis;
import com.github.mikephil.charting.data.Entry;
import com.github.mikephil.charting.data.LineData;
import com.github.mikephil.charting.data.LineDataSet;
import com.github.mikephil.charting.formatter.ValueFormatter;
import com.jins_jp.meme.academic.MainBleActivity;
import com.jins_jp.meme.academic.R;

public class GraphEOG {

    Context context;
    Activity activity;
    Handler handler;

    private LineChart graphicalView;

    private final int graph_width = 200;
    public static int GrasphSkipCountCull = 25;
    private int elapsedTime = 0;

    public GraphEOG(Context context, Activity activity, Handler handler) {
        this.context = context;
        this.activity = activity;
        this.handler = handler;
    }

    public void makeChart() {
        // 棒グラフ
        LineChart gView = activity.findViewById(R.id.graph_VvVh);
//        gView.setBackgroundColor(Color.WHITE);
        gView.getDescription().setEnabled(false);

        LineData data = new LineData();

        LineDataSet datasetVv = new LineDataSet(null, "Vv");
        datasetVv.setMode(LineDataSet.Mode.LINEAR);
        datasetVv.setDrawFilled(false);
        datasetVv.setDrawCircles(false);
        datasetVv.setDrawValues(false);
        datasetVv.setColor(Color.BLUE);

        data.addDataSet(datasetVv);

        LineDataSet datasetVh = new LineDataSet(null, "Vh");
        datasetVh.setMode(LineDataSet.Mode.LINEAR);
        datasetVh.setDrawFilled(false);
        datasetVh.setDrawCircles(false);
        datasetVh.setDrawValues(false);
        datasetVh.setColor(Color.RED);

        data.addDataSet(datasetVh);
        gView.setData(data);

        // X軸
        XAxis xl = gView.getXAxis();
        xl.setTextColor(Color.BLACK);
        xl.setLabelCount(9, true);
        xl.setAxisMaximum(200f);
        xl.setAxisMinimum(0f);
        xl.setPosition(XAxis.XAxisPosition.BOTTOM);
        xl.setValueFormatter(new ValueFormatter() {
            @Override
            public String getFormattedValue(float value) {
                Integer time = (int) (value/GrasphSkipCountCull+elapsedTime);
                return time.toString();
            }
        });

        // Y軸
        YAxis leftAxis = gView.getAxisLeft();
        leftAxis.setTextColor(Color.BLACK);
        leftAxis.setLabelCount(9, true);
        leftAxis.setDrawGridLines(true);
        leftAxis.setAxisMaximum(400f);
        leftAxis.setAxisMinimum(-400f);

        graphicalView = gView;
    }

    public void setGraphEOG(long cnt, short Vv, short Vh) {
        if ((cnt % graph_width) == 0) {
            clearGraphEOG();
            elapsedTime = elapsedTime + graph_width/GrasphSkipCountCull;
        }

        XAxis xl = graphicalView.getXAxis();
        xl.setValueFormatter(new ValueFormatter() {
            @Override
            public String getFormattedValue(float value) {
                Integer time = (int) (value/GrasphSkipCountCull+elapsedTime);
                return time.toString();
            }
        });

        graphicalView.getLineData().getDataSetByIndex(1).addEntry(new Entry(cnt % graph_width, Vv));
        graphicalView.getLineData().getDataSetByIndex(0).addEntry(new Entry(cnt % graph_width, Vh));

        graphicalView.getLineData().setHighlightEnabled(false);

        graphicalView.notifyDataSetChanged();
        graphicalView.invalidate();
    }

    public void clearGraphEOG() {
        graphicalView.getLineData().getDataSetByIndex(1).clear();
        graphicalView.getLineData().getDataSetByIndex(0).clear();
    }

    public void resetGraphEOG() {
        elapsedTime = 0;
        clearGraphEOG();
    }
}
