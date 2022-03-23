
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
import com.jins_jp.meme.academic.R;

public class GraphAcc {

    Context context;
    Activity activity;
    Handler handler;

    private LineChart graphicalView;

    private final int graph_width = 200;

    public GraphAcc(Context context, Activity activity, Handler handler) {
        this.context = context;
        this.activity = activity;
        this.handler = handler;
    }

    public void makeChart(){
        LineChart gView =  activity.findViewById(R.id.graph_acc);
        gView.setBackgroundColor(Color.WHITE);
        gView.getDescription().setEnabled(false);

        LineData data = new LineData();

        LineDataSet datasetAccX = new LineDataSet(null, "AccX");
        datasetAccX.setMode(LineDataSet.Mode.LINEAR);
        datasetAccX.setDrawFilled(false);
        datasetAccX.setDrawCircles(false);
        datasetAccX.setDrawValues(false);
        datasetAccX.setColor(Color.BLUE);

        data.addDataSet(datasetAccX);

        LineDataSet datasetAxxY = new LineDataSet(null, "AxxY");
        datasetAxxY.setMode(LineDataSet.Mode.LINEAR);
        datasetAxxY.setDrawFilled(false);
        datasetAxxY.setDrawCircles(false);
        datasetAxxY.setDrawValues(false);
        datasetAxxY.setColor(Color.GREEN);

        data.addDataSet(datasetAxxY);

        LineDataSet datasetAccZ = new LineDataSet(null, "AccZ");
        datasetAccZ.setMode(LineDataSet.Mode.LINEAR);
        datasetAccZ.setDrawFilled(false);
        datasetAccZ.setDrawCircles(false);
        datasetAccZ.setDrawValues(false);
        datasetAccZ.setColor(Color.RED);

        data.addDataSet(datasetAccZ);

        gView.setData(data);

        // X軸
        XAxis xl = gView.getXAxis();
        xl.setTextColor(Color.BLACK);
        xl.setLabelCount(11, true);
        xl.setAxisMaximum(200f);
        xl.setAxisMinimum(0f);
        xl.setPosition(XAxis.XAxisPosition.BOTTOM);

        // Y軸
        YAxis leftAxis = gView.getAxisLeft();
        leftAxis.setTextColor(Color.BLACK);
        leftAxis.setLabelCount(9, true);
        leftAxis.setDrawGridLines(true);
        leftAxis.setAxisMaximum(35000f);
        leftAxis.setAxisMinimum(-35000f);

        graphicalView = gView;
    }

    public void setGraphAcc(int cnt, short x, short y, short z) {
        if ((cnt % graph_width) == 0) {
            graphicalView.getLineData().getDataSetByIndex(0).clear();
            graphicalView.getLineData().getDataSetByIndex(1).clear();
            graphicalView.getLineData().getDataSetByIndex(2).clear();
        }
        graphicalView.getLineData().getDataSetByIndex(0).addEntry(new Entry(cnt % graph_width, x));
        graphicalView.getLineData().getDataSetByIndex(1).addEntry(new Entry(cnt % graph_width, y));
        graphicalView.getLineData().getDataSetByIndex(2).addEntry(new Entry(cnt % graph_width, z));

        graphicalView.getLineData().setHighlightEnabled(false);

        graphicalView.notifyDataSetChanged();
        graphicalView.invalidate();
    }
}
