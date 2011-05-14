#pragma once

#include "clang-c\Index.h"
using namespace System;

const char* ToCString(String ^str);
String^ ToManagedString(const CXString &str);
String^ ToManagedString(const char* str);

void Cleanup(const char* str);