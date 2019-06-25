
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

public class GraphGyro {

    Context context;
    Activity activity;
    Handler handler;

    private LineChart graphicalView;

    private final int graph_width = 50;

    public GraphGyro(Context context, Activity activity, Handler handler) {
        this.context = context;
        this.activity = activity;
        this.handler = handler;
    }



    public void makeChart(){
        // 棒グラフ
        LineChart gView = activity.findViewById(R.id.graph_gyro);
        gView.setBackgroundColor(Color.WHITE);
        gView.getDescription().setEnabled(false);

        LineData data = new LineData();

        LineDataSet datasetGyroX = new LineDataSet(null, "GyroX");
        datasetGyroX.setMode(LineDataSet.Mode.LINEAR);
        datasetGyroX.setDrawFilled(false);
        datasetGyroX.setDrawCircles(false);
        datasetGyroX.setDrawValues(false);
        datasetGyroX.setColor(Color.BLUE);

        data.addDataSet(datasetGyroX);

        LineDataSet datasetGyroY = new LineDataSet(null, "GyroY");
        datasetGyroY.setMode(LineDataSet.Mode.LINEAR);
        datasetGyroY.setDrawFilled(false);
        datasetGyroY.setDrawCircles(false);
        datasetGyroY.setDrawValues(false);
        datasetGyroY.setColor(Color.GREEN);

        data.addDataSet(datasetGyroY);

        LineDataSet datasetGyroZ = new LineDataSet(null, "GyroZ");
        datasetGyroZ.setMode(LineDataSet.Mode.LINEAR);
        datasetGyroZ.setDrawFilled(false);
        datasetGyroZ.setDrawCircles(false);
        datasetGyroZ.setDrawValues(false);
        datasetGyroZ.setColor(Color.RED);

        data.addDataSet(datasetGyroZ);


        gView.setData(data);

        // X軸
        XAxis xl = gView.getXAxis();
        xl.setTextColor(Color.BLACK);
        xl.setLabelCount(11, true);
        xl.setAxisMaximum(50f);
        xl.setAxisMinimum(0f);
        xl.setPosition(XAxis.XAxisPosition.BOTTOM);

        // Y軸
        YAxis leftAxis = gView.getAxisLeft();
        leftAxis.setTextColor(Color.BLACK);
        leftAxis.setLabelCount(9, true);
        leftAxis.setDrawGridLines(true);
        leftAxis.setAxisMaximum(65535f);
        leftAxis.setAxisMinimum(-65536f);

        graphicalView = gView;

    }

    public void setGraphGyro(int cnt, short x, short y, short z) {
        if ((cnt % graph_width) == 0) {
            graphicalView.getLineData().getDataSetByIndex(0).clear();
            graphicalView.getLineData().getDataSetByIndex(1).clear();
            graphicalView.getLineData().getDataSetByIndex(2).clear();
        }
        graphicalView.getLineData().getDataSetByIndex(0).addEntry(new Entry(cnt % graph_width, x));
        graphicalView.getLineData().getDataSetByIndex(1).addEntry(new Entry(cnt % graph_width, y));
        graphicalView.getLineData().getDataSetByIndex(2).addEntry(new Entry(cnt % graph_width, z));

        graphicalView.notifyDataSetChanged();
        graphicalView.invalidate();
    }
}
