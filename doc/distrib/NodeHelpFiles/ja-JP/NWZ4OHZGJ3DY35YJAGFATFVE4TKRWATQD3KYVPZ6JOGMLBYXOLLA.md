<!--- Autodesk.DesignScript.Geometry.Curve.ExtrudeAsSolid(curve, distance) --->
<!--- NWZ4OHZGJ3DY35YJAGFATFVE4TKRWATQD3KYVPZ6JOGMLBYXOLLA --->
## 詳細
`Curve.ExtrudeAsSolid (curve, distance)` は、入力された数値を使用して押し出す方向を決定し、入力された閉じた平面曲線を押し出します。押し出す方向は、曲線が配置されている平面の法線ベクトルによって決まります。このノードは、押し出しの端点を塞いでソリッドを作成します。

次の例では、まず `NurbsCurve.ByPoints` ノードを使用して、ランダムに生成された一連の点を入力として持つ NURBS 曲線を作成します。次に、`Curve.ExtrudeAsSolid` ノードを使用して曲線をソリッドとして押し出します。数値スライダは、`Curve.ExtrudeAsSolid` の `distance` 入力に使用されます。
___
## サンプル ファイル

![Curve.ExtrudeAsSolid(curve, distance)](./NWZ4OHZGJ3DY35YJAGFATFVE4TKRWATQD3KYVPZ6JOGMLBYXOLLA_img.jpg)
