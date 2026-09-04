package com.cellscope.mobile;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import org.json.JSONObject;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;

public class CollectorService extends Service implements LocationListener {

    private static final String CHANNEL_ID = "CellScope_Collector";
    private Handler handler;
    private Runnable runnable;
    private LocationManager locationManager;
    private double currentLat = 18.5204;
    private double currentLon = 73.8567;
    private double currentAlt = 560.0;
    private double currentAccuracy = 4.5;
    private int uploadCount = 0;
    public static boolean isRunning = false;
    public static String lastStatus = "Idle";

    @Override
    public void onCreate() {
        super.onCreate();
        createNotificationChannel();
        handler = new Handler(Looper.getMainLooper());
        locationManager = (LocationManager) getSystemService(Context.LOCATION_SERVICE);
        try {
            if (checkSelfPermission(android.Manifest.permission.ACCESS_FINE_LOCATION) == android.content.pm.PackageManager.PERMISSION_GRANTED) {
                locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 2000, 1, this);
                locationManager.requestLocationUpdates(LocationManager.NETWORK_PROVIDER, 2000, 1, this);
            }
        } catch (Exception ignored) {}
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        isRunning = true;
        startForeground(1001, buildNotification("● CellScope Active — Ingesting 5G/4G Telemetry"));

        runnable = new Runnable() {
            @Override
            public void run() {
                if (!isRunning) return;
                performTick();
                handler.postDelayed(this, 3000); // Ingest every 3 seconds
            }
        };
        handler.post(runnable);
        return START_STICKY;
    }

    private void performTick() {
        new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    TelephonyHelper.CellData data = TelephonyHelper.readCellInfo(getApplicationContext());
                    SharedPreferences prefs = getSharedPreferences("cellscope_prefs", Context.MODE_PRIVATE);
                    String backendUrl = prefs.getString("backend_url", "http://192.168.31.157:5050");
                    String deviceId = prefs.getString("device_id", "00000000-0000-0000-0000-000000000001");

                    JSONObject payload = data.toJson(currentLat, currentLon, currentAlt, currentAccuracy, deviceId);

                    URL url = new URL(backendUrl + "/api/cellular/snapshot");
                    HttpURLConnection conn = (HttpURLConnection) url.openConnection();
                    conn.setRequestMethod("POST");
                    conn.setRequestProperty("Content-Type", "application/json");
                    conn.setConnectTimeout(2500);
                    conn.setReadTimeout(2500);
                    conn.setDoOutput(true);

                    OutputStream os = conn.getOutputStream();
                    os.write(payload.toString().getBytes("UTF-8"));
                    os.flush();
                    os.close();

                    int code = conn.getResponseCode();
                    if (code >= 200 && code < 300) {
                        uploadCount++;
                        lastStatus = "✓ Live Upload #" + uploadCount + " (" + data.radioTech + " • " + data.cellId + ")";
                    } else {
                        lastStatus = "Server HTTP " + code;
                    }
                    conn.disconnect();
                } catch (Exception e) {
                    lastStatus = "Connecting: " + e.getMessage();
                }
            }
        }).start();
    }

    private Notification buildNotification(String text) {
        Intent intent = new Intent(this, MainActivity.class);
        PendingIntent pendingIntent = PendingIntent.getActivity(this, 0, intent, PendingIntent.FLAG_IMMUTABLE);

        Notification.Builder builder;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            builder = new Notification.Builder(this, CHANNEL_ID);
        } else {
            builder = new Notification.Builder(this);
        }

        return builder.setContentTitle("CellScope Mobile Collector")
                .setContentText(text)
                .setSmallIcon(android.R.drawable.ic_menu_compass)
                .setContentIntent(pendingIntent)
                .setOngoing(true)
                .build();
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(
                    CHANNEL_ID,
                    "CellScope Telemetry Collector",
                    NotificationManager.IMPORTANCE_LOW
            );
            channel.setDescription("Continuous cellular radio & GPS telemetry collection");
            NotificationManager nm = (NotificationManager) getSystemService(NotificationManager.class);
            if (nm != null) nm.createNotificationChannel(channel);
        }
    }

    @Override
    public void onDestroy() {
        isRunning = false;
        if (handler != null && runnable != null) handler.removeCallbacks(runnable);
        if (locationManager != null) locationManager.removeUpdates(this);
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) { return null; }

    @Override
    public void onLocationChanged(Location location) {
        if (location != null) {
            currentLat = location.getLatitude();
            currentLon = location.getLongitude();
            currentAlt = location.getAltitude();
            currentAccuracy = location.getAccuracy();
        }
    }

    @Override public void onStatusChanged(String provider, int status, Bundle extras) {}
    @Override public void onProviderEnabled(String provider) {}
    @Override public void onProviderDisabled(String provider) {}
}
