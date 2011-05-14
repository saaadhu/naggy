#include "Stdafx.h"
#include "StringUtilities.h"

using namespace System::Runtime::InteropServices;

const char* ToCString(String ^str)
{
	System::IntPtr ptr = Marshal::StringToHGlobalAnsi(str);
	return (const char *)ptr.ToPointer();
}

String^ ToManagedString(const char* str)
{
	String^ managedString = Marshal::PtrToStringAnsi(System::IntPtr((int)str));
	return managedString;
}

String^ ToManagedString(const CXString &str)
{
	const char* strData = clang_getCString(str);
	return ToManagedString(strData);	
}

void Cleanup(const char* str)
{
	Marshal::FreeHGlobal(IntPtr((void *)str));
}