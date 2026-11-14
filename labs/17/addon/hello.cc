#include <node.h>
// Built against the V8/Node C++ API, so it is tied to one NODE_MODULE_VERSION
void Hello(const v8::FunctionCallbackInfo<v8::Value>& args) {
  args.GetReturnValue().Set(v8::String::NewFromUtf8(args.GetIsolate(), "hello").ToLocalChecked());
}
void Init(v8::Local<v8::Object> exports) { NODE_SET_METHOD(exports, "hello", Hello); }
NODE_MODULE(NODE_GYP_MODULE_NAME, Init)
