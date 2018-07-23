// InstallNetFramework.cpp: 定义应用程序的入口点。
//

#include "stdafx.h"
#include "InstallNetFramework.h"
#include <Shellapi.h>
#include <math.h>
#define MAX_LOADSTRING 100

// 全局变量: 
HINSTANCE hInst;                                // 当前实例
WCHAR szTitle[MAX_LOADSTRING];                  // 标题栏文本
WCHAR szWindowClass[MAX_LOADSTRING];            // 主窗口类名


typedef BOOL(WINAPI *LPFN_ISWOW64PROCESS) (HANDLE, PBOOL);


BOOL IsWow64()
{
	BOOL bIsWow64 = FALSE;

	//IsWow64Process is not available on all supported versions of Windows.    
	//Use GetModuleHandle to get a handle to the DLL that contains the function    
	//and GetProcAddress to get a pointer to the function if available.    

	LPFN_ISWOW64PROCESS fnIsWow64Process = (LPFN_ISWOW64PROCESS)GetProcAddress(
		GetModuleHandle(TEXT("kernel32")), "IsWow64Process");

	if (NULL != fnIsWow64Process)
	{
		if (!fnIsWow64Process(GetCurrentProcess(), &bIsWow64))
		{
			//handle error    
		}
	}
	return bIsWow64;
}

bool isInstalled()
{
	double compareVersion = 4.6;
	
	

	/*LPWSTR *szArgList;
	int argCount;

	szArgList = CommandLineToArgvW(GetCommandLine(), &argCount);
	if (szArgList != NULL)
	{
	compareVersion = _wtof(szArgList[1]);
	ndpName = szArgList[2];
	appName = szArgList[3];
	}	*/

	bool installed = false;
	HKEY hKey;
	LPCTSTR path = L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full";
	long lRet = RegOpenKeyEx(HKEY_LOCAL_MACHINE, path, 0, KEY_WOW64_32KEY | KEY_QUERY_VALUE, &hKey);
	if (lRet == ERROR_SUCCESS)
	{
		CHAR   szData[100];

		DWORD   dwSize = 100;
		lRet = RegQueryValueExA(hKey, "Version", NULL, NULL, (LPBYTE)szData, &dwSize);
		if (lRet == ERROR_SUCCESS)
		{
			CHAR   szData2[100];
			int index = 0;
			for (int i = 0; i < dwSize; i++)
			{
				szData2[index++] = szData[i];
				if (szData[i] == '.')
				{
					i++;
					for (; i < dwSize; i++)
					{
						if (szData[i] == '.')
						{
						}
						else
						{
							szData2[index++] = szData[i];
						}
					}
					break;
				}
			}
			double version = atof(szData2);
			if (version >= compareVersion)
			{
				installed = true;
			}
		}
	}
	return installed;
}

// 此代码模块中包含的函数的前向声明: 
ATOM                MyRegisterClass(HINSTANCE hInstance);
BOOL                InitInstance(HINSTANCE, int);
LRESULT CALLBACK    WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK    About(HWND, UINT, WPARAM, LPARAM);

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                     _In_opt_ HINSTANCE hPrevInstance,
                     _In_ LPWSTR    lpCmdLine,
                     _In_ int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

	if (!IsWow64())
	{
		MessageBox(0, _T("请在64位windows上安装此软件"), _T("提示"), 0);
		return 0;
	}

	LPWSTR ndpName = L"package\\ndp462.exe";
	LPWSTR appName = L"package\\app.exe";

	if (isInstalled() == false)
	{
		STARTUPINFOW StartInfo;
		PROCESS_INFORMATION pinfo;
		//对程序的启动信息不作任何设定，全部清0
		memset(&StartInfo, 0, sizeof(STARTUPINFO));
		StartInfo.cb = sizeof(STARTUPINFO);//设定结构的大小
		BOOL ret = CreateProcessW(
			ndpName, //启动程序路径名
			NULL, //参数（当exeName为NULL时，可将命令放入参数前）
			NULL,  //使用默认进程安全属性
			NULL,  //使用默认线程安全属性
			FALSE,//句柄不继承
			NORMAL_PRIORITY_CLASS, //使用正常优先级
			NULL,  //使用父进程的环境变量
			NULL,  //指定工作目录
			&StartInfo, //子进程主窗口如何显示
			&pinfo); //用于存放新进程的返回信息
		if (ret)
		{
			WaitForSingleObject(pinfo.hProcess, INFINITE);
		}
	}

	if (isInstalled())
	{
		STARTUPINFOW StartInfo;
		PROCESS_INFORMATION pinfo;
		//对程序的启动信息不作任何设定，全部清0
		memset(&StartInfo, 0, sizeof(STARTUPINFO));
		StartInfo.cb = sizeof(STARTUPINFO);//设定结构的大小
		BOOL ret = CreateProcessW(
			appName, //启动程序路径名
			NULL, //参数（当exeName为NULL时，可将命令放入参数前）
			NULL,  //使用默认进程安全属性
			NULL,  //使用默认线程安全属性
			FALSE,//句柄不继承
			NORMAL_PRIORITY_CLASS, //使用正常优先级
			NULL,  //使用父进程的环境变量
			NULL,  //指定工作目录
			&StartInfo, //子进程主窗口如何显示
			&pinfo); //用于存放新进程的返回信息
	}
	else
	{
		MessageBox(0, _T(".Net Framework没有成功安装，安装程序将自动退出！"), _T("提示"), 0);
	}

	return 0;

    // TODO: 在此放置代码。

    // 初始化全局字符串
    LoadStringW(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
    LoadStringW(hInstance, IDC_INSTALLNETFRAMEWORK, szWindowClass, MAX_LOADSTRING);
    MyRegisterClass(hInstance);

    // 执行应用程序初始化: 
    if (!InitInstance (hInstance, nCmdShow))
    {
        return FALSE;
    }

    HACCEL hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_INSTALLNETFRAMEWORK));

    MSG msg;

    // 主消息循环: 
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    return (int) msg.wParam;
}



//
//  函数: MyRegisterClass()
//
//  目的: 注册窗口类。
//
ATOM MyRegisterClass(HINSTANCE hInstance)
{
    WNDCLASSEXW wcex;

    wcex.cbSize = sizeof(WNDCLASSEX);

    wcex.style          = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc    = WndProc;
    wcex.cbClsExtra     = 0;
    wcex.cbWndExtra     = 0;
    wcex.hInstance      = hInstance;
    wcex.hIcon          = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_INSTALLNETFRAMEWORK));
    wcex.hCursor        = LoadCursor(nullptr, IDC_ARROW);
    wcex.hbrBackground  = (HBRUSH)(COLOR_WINDOW+1);
    wcex.lpszMenuName   = MAKEINTRESOURCEW(IDC_INSTALLNETFRAMEWORK);
    wcex.lpszClassName  = szWindowClass;
    wcex.hIconSm        = LoadIcon(wcex.hInstance, MAKEINTRESOURCE(IDI_SMALL));

    return RegisterClassExW(&wcex);
}

//
//   函数: InitInstance(HINSTANCE, int)
//
//   目的: 保存实例句柄并创建主窗口
//
//   注释: 
//
//        在此函数中，我们在全局变量中保存实例句柄并
//        创建和显示主程序窗口。
//
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
   hInst = hInstance; // 将实例句柄存储在全局变量中

   HWND hWnd = CreateWindowW(szWindowClass, szTitle, WS_OVERLAPPEDWINDOW,
      CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, nullptr, nullptr, hInstance, nullptr);

   if (!hWnd)
   {
      return FALSE;
   }

   ShowWindow(hWnd, nCmdShow);
   UpdateWindow(hWnd);

   return TRUE;
}

//
//  函数: WndProc(HWND, UINT, WPARAM, LPARAM)
//
//  目的:    处理主窗口的消息。
//
//  WM_COMMAND  - 处理应用程序菜单
//  WM_PAINT    - 绘制主窗口
//  WM_DESTROY  - 发送退出消息并返回
//
//
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
    case WM_COMMAND:
        {
            int wmId = LOWORD(wParam);
            // 分析菜单选择: 
            switch (wmId)
            {
            case IDM_ABOUT:
                DialogBox(hInst, MAKEINTRESOURCE(IDD_ABOUTBOX), hWnd, About);
                break;
            case IDM_EXIT:
                DestroyWindow(hWnd);
                break;
            default:
                return DefWindowProc(hWnd, message, wParam, lParam);
            }
        }
        break;
    case WM_PAINT:
        {
            PAINTSTRUCT ps;
            HDC hdc = BeginPaint(hWnd, &ps);
            // TODO: 在此处添加使用 hdc 的任何绘图代码...
            EndPaint(hWnd, &ps);
        }
        break;
    case WM_DESTROY:
        PostQuitMessage(0);
        break;
    default:
        return DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}

// “关于”框的消息处理程序。
INT_PTR CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
    UNREFERENCED_PARAMETER(lParam);
    switch (message)
    {
    case WM_INITDIALOG:
        return (INT_PTR)TRUE;

    case WM_COMMAND:
        if (LOWORD(wParam) == IDOK || LOWORD(wParam) == IDCANCEL)
        {
            EndDialog(hDlg, LOWORD(wParam));
            return (INT_PTR)TRUE;
        }
        break;
    }
    return (INT_PTR)FALSE;
}
