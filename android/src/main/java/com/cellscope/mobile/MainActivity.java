package com.cellscope.mobile;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.Typeface;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;
import org.json.JSONObject;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;

public class MainActivity extends Activity {

    private static final int PERMISSION_REQ_CODE = 101;
    private TextView tvStatus;
    private TextView tvServingCell;
    private TextView tvSignal;
    private TextView tvLocation;
    private TextView tvUploads;
    private Button btnToggleService;
    private Handler updateHandler;
    private Runnable updateRunnable;
    private SharedPreferences prefs;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        prefs = getSharedPreferences("cellscope_prefs", Context.MODE_PRIVATE);
        if (!prefs.contains("backend_url")) {
            prefs.edit().putString("backend_url", "http://192.168.31.157:5050").apply();
        }

        requestNecessaryPermissions();
        buildUi();

        updateHandler = new Handler(Looper.getMainLooper());
        updateRunnable = new Runnable() {
            @Override
            public void run() {
                refreshUiData();
                updateHandler.postDelayed(this, 1500);
            }
        };
        updateHandler.post(updateRunnable);
    }

    private void requestNecessaryPermissions() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            String[] perms = {
                    android.Manifest.permission.ACCESS_FINE_LOCATION,
                    android.Manifest.permission.ACCESS_COARSE_LOCATION,
                    android.Manifest.permission.READ_PHONE_STATE
            };
            requestPermissions(perms, PERMISSION_REQ_CODE);
        }
    }

    private void buildUi() {
        ScrollView scroll = new ScrollView(this);
        scroll.setBackgroundColor(Color.parseColor("#0b0f19"));
        scroll.setLayoutParams(new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setPadding(36, 48, 36, 48);

        // Header Title
        TextView tvTitle = new TextView(this);
        tvTitle.setText("📡 CellScope Mobile");
        tvTitle.setTextColor(Color.parseColor("#06b6d4"));
        tvTitle.setTextSize(24);
        tvTitle.setTypeface(null, Typeface.BOLD);
        root.addView(tvTitle);

        TextView tvSub = new TextView(this);
        tvSub.setText("Carrier 5G/4G Hardware Telemetry Collector");
        tvSub.setTextColor(Color.parseColor("#94a3b8"));
        tvSub.setTextSize(13);
        tvSub.setPadding(0, 4, 0, 24);
        root.addView(tvSub);

        // Status Banner
        tvStatus = createCardTextView(root, "● System Ready", "#10b981", "#131c2e");

        // Serving Cell Card
        root.addView(createSectionHeader("🗼 SERVING CELL TELEMETRY"));
        tvServingCell = createCardTextView(root, "Scanning cellular base stations...", "#f8fafc", "#131c2e");

        // Signal Card
        root.addView(createSectionHeader("📶 RADIO SIGNAL & QUALITY"));
        tvSignal = createCardTextView(root, "Acquiring RSRP/RSRQ/SINR...", "#f8fafc", "#131c2e");

        // Telemetry Uploads Card
        root.addView(createSectionHeader("🚀 LIVE BACKEND INGESTION"));
        tvUploads = createCardTextView(root, "Status: Idle\nTarget: " + prefs.getString("backend_url", "http://192.168.31.157:5050"), "#f8fafc", "#131c2e");

        // Action Buttons
        LinearLayout btnLayout = new LinearLayout(this);
        btnLayout.setOrientation(LinearLayout.VERTICAL);
        btnLayout.setPadding(0, 20, 0, 10);

        btnToggleService = createButton("▶ START 5G BACKGROUND INGESTION", "#06b6d4", Color.parseColor("#0b0f19"));
        btnToggleService.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                toggleCollectorService();
            }
        });
        btnLayout.addView(btnToggleService);

        Button btnPair = createButton("🔗 PAIR WITH WEB CONSOLE", "#f59e0b", Color.parseColor("#0b0f19"));
        btnPair.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showPairingDialog();
            }
        });
        btnLayout.addView(btnPair);

        Button btnOpenWeb = createButton("🗺️ OPEN GIS MAP & WEB CONSOLE", "#10b981", Color.parseColor("#0b0f19"));
        btnOpenWeb.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                String url = prefs.getString("backend_url", "http://192.168.31.157:5050");
                Intent i = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
                startActivity(i);
            }
        });
        btnLayout.addView(btnOpenWeb);

        Button btnConfig = createButton("⚙️ CONFIGURE SERVER IP", "#64748b", Color.WHITE);
        btnConfig.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showServerConfigDialog();
            }
        });
        btnLayout.addView(btnConfig);

        root.addView(btnLayout);

        // Developer Attribution Footer
        TextView tvFooter = new TextView(this);
        tvFooter.setText("Developed by Shatrughna Ambhore\nambhoreshatrughna@gmail.com • +91 9604466334");
        tvFooter.setTextColor(Color.parseColor("#64748b"));
        tvFooter.setTextSize(12);
        tvFooter.setGravity(Gravity.CENTER);
        tvFooter.setPadding(0, 30, 0, 30);
        root.addView(tvFooter);

        scroll.addView(root);
        setContentView(scroll);
    }

    private TextView createSectionHeader(String title) {
        TextView tv = new TextView(this);
        tv.setText(title);
        tv.setTextColor(Color.parseColor("#64748b"));
        tv.setTextSize(11);
        tv.setTypeface(null, Typeface.BOLD);
        tv.setPadding(0, 16, 0, 6);
        return tv;
    }

    private TextView createCardTextView(LinearLayout parent, String initialText, String textColor, String bgColor) {
        TextView tv = new TextView(this);
        tv.setText(initialText);
        tv.setTextColor(Color.parseColor(textColor));
        tv.setBackgroundColor(Color.parseColor(bgColor));
        tv.setTextSize(13);
        tv.setPadding(24, 20, 24, 20);
        tv.setLineSpacing(4, 1.1f);
        parent.addView(tv);
        return tv;
    }

    private Button createButton(String text, String bgColor, int textColor) {
        Button b = new Button(this);
        b.setText(text);
        b.setBackgroundColor(Color.parseColor(bgColor));
        b.setTextColor(textColor);
        b.setTextSize(13);
        b.setTypeface(null, Typeface.BOLD);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        lp.setMargins(0, 8, 0, 8);
        b.setLayoutParams(lp);
        return b;
    }

    private void toggleCollectorService() {
        Intent serviceIntent = new Intent(this, CollectorService.class);
        if (CollectorService.isRunning) {
            stopService(serviceIntent);
            btnToggleService.setText("▶ START 5G BACKGROUND INGESTION");
            btnToggleService.setBackgroundColor(Color.parseColor("#06b6d4"));
            Toast.makeText(this, "Collection Paused", Toast.LENGTH_SHORT).show();
        } else {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(serviceIntent);
            } else {
                startService(serviceIntent);
            }
            btnToggleService.setText("⏹ STOP TELEMETRY INGESTION");
            btnToggleService.setBackgroundColor(Color.parseColor("#ef4444"));
            Toast.makeText(this, "5G Telemetry Ingestion Started!", Toast.LENGTH_SHORT).show();
        }
    }

    private void refreshUiData() {
        TelephonyHelper.CellData cell = TelephonyHelper.readCellInfo(this);

        tvServingCell.setText("Operator: " + cell.operatorName + " (MCC " + cell.mcc + " MNC " + cell.mnc + ")\n"
                + "Technology: " + cell.radioTech + " • Band: " + cell.band + "\n"
                + "Cell ID: " + cell.cellId + " • PCI: " + cell.physicalCellId + "\n"
                + "Tracking Area (TAC): " + cell.tac);

        tvSignal.setText("RSRP (Signal): " + cell.rsrp + " dBm (Level " + cell.signalLevel + "/4)\n"
                + "RSRQ (Quality): " + cell.rsrq + " dB\n"
                + "SINR / RSSNR: " + cell.sinr + " dB\n"
                + "Neighbors: " + cell.neighborCells.length() + " adjacent base stations");

        if (CollectorService.isRunning) {
            tvStatus.setText("● Active Collecting (Live Stream to Server)");
            tvStatus.setTextColor(Color.parseColor("#10b981"));
            tvUploads.setText("Stream: ACTIVE\nStatus: " + CollectorService.lastStatus + "\nTarget: " + prefs.getString("backend_url", "http://192.168.31.157:5050"));
        } else {
            tvStatus.setText("○ Standby — Ready to Ingest");
            tvStatus.setTextColor(Color.parseColor("#f59e0b"));
            tvUploads.setText("Stream: IDLE\nTarget: " + prefs.getString("backend_url", "http://192.168.31.157:5050"));
        }
    }

    private void showPairingDialog() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setTitle("Pair Device With Web Console");
        builder.setMessage("Enter the 8-character Pairing Code shown on your Web or Desktop app (/devices):");

        final EditText input = new EditText(this);
        input.setHint("e.g. 7X9K2M4P");
        builder.setView(input);

        builder.setPositiveButton("Confirm Pairing", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                final String code = input.getText().toString().trim();
                if (code.isEmpty()) return;
                new Thread(new Runnable() {
                    @Override
                    public void run() {
                        confirmPairing(code);
                    }
                }).start();
            }
        });
        builder.setNegativeButton("Cancel", null);
        builder.show();
    }

    private void confirmPairing(String code) {
        try {
            String backendUrl = prefs.getString("backend_url", "http://192.168.31.157:5050");
            URL url = new URL(backendUrl + "/api/devices/pair/confirm");
            HttpURLConnection conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("POST");
            conn.setRequestProperty("Content-Type", "application/json");
            conn.setDoOutput(true);

            JSONObject req = new JSONObject();
            req.put("pairingCode", code);
            req.put("deviceName", Build.MODEL + " (Samsung 5G)");
            req.put("platform", "Android");
            req.put("model", Build.MANUFACTURER + " " + Build.MODEL + " / Android " + Build.VERSION.RELEASE);

            OutputStream os = conn.getOutputStream();
            os.write(req.toString().getBytes("UTF-8"));
            os.flush();
            os.close();

            int status = conn.getResponseCode();
            if (status >= 200 && status < 300) {
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        Toast.makeText(MainActivity.this, "🎉 Device Paired Successfully with Backend!", Toast.LENGTH_LONG).show();
                    }
                });
            } else {
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        Toast.makeText(MainActivity.this, "Pairing failed. Check code in Web Console.", Toast.LENGTH_LONG).show();
                    }
                });
            }
            conn.disconnect();
        } catch (final Exception e) {
            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(MainActivity.this, "Error: " + e.getMessage(), Toast.LENGTH_LONG).show();
                }
            });
        }
    }

    private void showServerConfigDialog() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder.setTitle("Configure CellScope Server URL");
        final EditText input = new EditText(this);
        input.setText(prefs.getString("backend_url", "http://192.168.31.157:5050"));
        builder.setView(input);

        builder.setPositiveButton("Save", new DialogInterface.OnClickListener() {
            @Override
            public void onClick(DialogInterface dialog, int which) {
                String newUrl = input.getText().toString().trim();
                if (!newUrl.isEmpty()) {
                    prefs.edit().putString("backend_url", newUrl).apply();
                    Toast.makeText(MainActivity.this, "Server URL updated!", Toast.LENGTH_SHORT).show();
                }
            }
        });
        builder.setNegativeButton("Cancel", null);
        builder.show();
    }

    @Override
    protected void onDestroy() {
        if (updateHandler != null && updateRunnable != null) {
            updateHandler.removeCallbacks(updateRunnable);
        }
        super.onDestroy();
    }
}
