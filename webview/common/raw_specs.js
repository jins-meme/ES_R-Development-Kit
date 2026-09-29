// 生波形のグラフ(EOG Vv・Vh / 加速度 / 角速度)。標準版はこれだけを描き、高機能版は判定しない計測条件のときにこれを描く。
// データがある列だけ描く(Standard モードは角速度なし、Quaternion は描くものなし)。仕様は webview/README.md。

// 既定の縦の範囲(今の Mac / Windows 版の ±1200 LSB・±8000 LSB に近いところ)
const EOG_DEF = [-600, 600];
const ACC_DEF = [-2, 2];         // G
const GYRO_DEF = [-250, 250];    // dps

/** 表示する範囲の行を読むための使い回しの配列(毎コマ作り直さない) */
export function makeBufs() {
  const bufs = new Map();
  const buf = (key, n, Type = Float32Array) => {
    let b = bufs.get(key);
    if (!b || b.length < n) { b = new Type(Math.max(n, 1024) * 1.5 | 0); bufs.set(key, b); }
    return b.subarray(0, n);
  };
  /** 行 [r0, r1) の横軸(秒) */
  const xs = (store, r0, r1, key = "x") => {
    const x = buf(key, r1 - r0, Float64Array);
    for (let r = r0, k = 0; r < r1; r++, k++) x[k] = r / store.cps;
    return x;
  };
  return { buf, xs };
}

/** 生波形の 3 枚。ctx = { store, cond }(viewer.js の specs に渡るもの) */
export function rawSpecs({ store, cond }) {
  const { buf, xs } = makeBufs();
  const acc = (cond.accRange ?? 8) / 32768, gyro = (cond.gyroRange ?? 1000) / 32768;
  const off = cond.accOffset ?? [0, 0, 0];
  return [
    {
      key: "eog", title: "EOG", unit: "µV", def: EOG_DEF, dp: 0,
      series: [{ name: "Vv", color: "--c-vv" }, { name: "Vh", color: "--c-vh" }],
      data(t0, t1) {
        if (store.has("EOG_V")) {                           // Full: 1 行に 1 点
          const [r0, r1] = store.span(t0, t1), n = r1 - r0;
          return [xs(store, r0, r1), store.read("EOG_V", r0, r1, buf("v", n)), store.read("EOG_H", r0, r1, buf("h", n))];
        }
        if (store.has("EOG_V1")) {                          // Standard: 1 行に 2 点(EOG は加速度の 2 倍の速さ)
          const [r0, r1] = store.span(t0, t1), n = r1 - r0;
          const x = buf("x2", 2 * n, Float64Array), v = buf("v2", 2 * n), h = buf("h2", 2 * n);
          for (let r = r0, k = 0; r < r1; r++, k += 2) {
            x[k] = r / store.cps; x[k + 1] = (r + 0.5) / store.cps;
            v[k] = store.value("EOG_V1", r); v[k + 1] = store.value("EOG_V2", r);
            h[k] = store.value("EOG_H1", r); h[k + 1] = store.value("EOG_H2", r);
          }
          return [x, v, h];
        }
        return null;
      },
    },
    {
      key: "acc", title: "Accelerometer", unit: "G", def: ACC_DEF, dp: 2,
      series: [{ name: "X", color: "--c-x" }, { name: "Y", color: "--c-y" }, { name: "Z", color: "--c-z" }],
      data(t0, t1) {
        if (!store.has("ACC_X")) return null;
        const [r0, r1] = store.span(t0, t1), n = r1 - r0;
        return [xs(store, r0, r1), store.read("ACC_X", r0, r1, buf("ax", n), acc, off[0]),
                store.read("ACC_Y", r0, r1, buf("ay", n), acc, off[1]), store.read("ACC_Z", r0, r1, buf("az", n), acc, off[2])];
      },
    },
    {
      key: "gyro", title: "Gyroscope", unit: "dps", def: GYRO_DEF, dp: 0,
      series: [{ name: "X", color: "--c-x" }, { name: "Y", color: "--c-y" }, { name: "Z", color: "--c-z" }],
      data(t0, t1) {
        if (!store.has("GYRO_X")) return null;
        const [r0, r1] = store.span(t0, t1), n = r1 - r0;
        return [xs(store, r0, r1), store.read("GYRO_X", r0, r1, buf("gx", n), gyro),
                store.read("GYRO_Y", r0, r1, buf("gy", n), gyro), store.read("GYRO_Z", r0, r1, buf("gz", n), gyro)];
      },
    },
  ];
}
