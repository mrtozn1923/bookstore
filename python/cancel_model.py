"""Post 11: the cancellation model of posts 07 and 09 in scikit-learn, exported to ONNX.
Usage (from the repo root): python python/cancel_model.py"""
import os

import numpy as np
import onnxruntime as ort
import pandas as pd
from skl2onnx import to_onnx
from skl2onnx.common.data_types import FloatTensorType, StringTensorType
from sklearn.compose import ColumnTransformer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import average_precision_score, roc_auc_score
from sklearn.model_selection import GroupKFold, GroupShuffleSplit, cross_val_score
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import FunctionTransformer, OneHotEncoder, StandardScaler

CATEGORICAL = ["PaymentMethod", "ShippingMethod", "Warehouse"]
NUMERIC = ["ItemCount", "OrderTotal", "DiscountRate", "PreviousOrders", "IsCampaign", "HasBackorderItem", "IsFirstOrder"]

# Same features as post 07: PreviousOrders counts only the customer's earlier orders
orders = pd.read_csv("data/orders_clean.csv", parse_dates=["OrderedAtUtc"]).sort_values("OrderedAtUtc", kind="stable")
orders["PreviousOrders"] = orders.groupby("CustomerId").cumcount()
orders["IsFirstOrder"] = orders["PreviousOrders"] == 0
for column in NUMERIC:
    orders[column] = orders[column].astype(np.float32)
X, y, groups = orders[CATEGORICAL + NUMERIC], orders["IsCancelled"], orders["CustomerId"]

# 80/20 split grouped by customer (the counterpart of samplingKeyColumnName)
train_idx, test_idx = next(GroupShuffleSplit(n_splits=1, test_size=0.2, random_state=7).split(X, y, groups))
X_train, X_test, y_train, y_test = X.iloc[train_idx], X.iloc[test_idx], y.iloc[train_idx], y.iloc[test_idx]
print(f"train {len(X_train):,} orders ({y_train.mean():.1%} cancelled), test {len(X_test):,} orders ({y_test.mean():.1%} cancelled)")

pipeline = Pipeline([
    ("features", ColumnTransformer([
        ("onehot", OneHotEncoder(handle_unknown="ignore"), CATEGORICAL),
        ("scale", StandardScaler(), NUMERIC),
    ])),
    ("model", LogisticRegression(max_iter=1000)),
])

cv = cross_val_score(pipeline, X_train, y_train, groups=groups.iloc[train_idx], cv=GroupKFold(n_splits=5), scoring="roc_auc")
print(f"5-fold CV AUC {cv.mean():.3f} ± {cv.std():.3f}")

pipeline.fit(X_train, y_train)
p_sklearn = pipeline.predict_proba(X_test)[:, 1]
print(f"test AUC {roc_auc_score(y_test, p_sklearn):.3f}, PR-AUC {average_precision_score(y_test, p_sklearn):.3f}")

# Export: one ONNX input per column, preprocessing included in the graph, plain probability output
inputs = [(c, StringTensorType([None, 1])) for c in CATEGORICAL] + [(c, FloatTensorType([None, 1])) for c in NUMERIC]
onx = to_onnx(pipeline, initial_types=inputs, target_opset=18, options={LogisticRegression: {"zipmap": False}})
os.makedirs("models", exist_ok=True)
with open("models/cancel.onnx", "wb") as f:
    f.write(onx.SerializeToString())
print(f"models/cancel.onnx: {os.path.getsize('models/cancel.onnx'):,} bytes, opsets "
      + ", ".join(sorted(f"{o.domain or 'ai.onnx'} {o.version}" for o in onx.opset_import)))

# The same rows through ONNX Runtime in Python: float32 inside the graph, float64 in scikit-learn
session = ort.InferenceSession("models/cancel.onnx", providers=["CPUExecutionProvider"])
feed = {c: X_test[[c]].to_numpy().astype(str) for c in CATEGORICAL} | {c: X_test[[c]].to_numpy() for c in NUMERIC}
label, probabilities = session.run(None, feed)
p_onnx = probabilities[:, 1]
print(f"outputs: {[o.name for o in session.get_outputs()]}; max |sklearn - onnx| = {np.abs(p_sklearn - p_onnx).max():.2e}")

# Rows and scikit-learn probabilities for the .NET check
out = X_test.copy()
out["SklearnProbability"] = p_sklearn
out.to_csv("models/cancel_test.csv", index=False, float_format="%.9g")

# A custom step that skl2onnx does not know
custom = Pipeline([("log", FunctionTransformer(np.log1p)), ("model", LogisticRegression(max_iter=1000))])
custom.fit(X_train[["OrderTotal"]], y_train)
try:
    to_onnx(custom, initial_types=[("OrderTotal", FloatTensorType([None, 1]))], target_opset=18)
except Exception as e:
    print(f"FunctionTransformer: {type(e).__name__}: {str(e).splitlines()[0][:110]}")
