// phash for zip files, using bit7z

#include <string>
#include <vector>
#include <algorithm>
#include <mutex>

#include <filesystem>
namespace fs = std::filesystem;

#include "dirent.h"
#include <sys/stat.h>
#include "direct.h"

#include "omp.h"
#pragma warning (disable : 4146)
#pragma warning (disable : 4267)
#pragma warning (disable : 4244)
#pragma warning (disable : 4319)
#include "CImg.h"
using namespace cimg_library;

#include <string>
#include <list>
#include <assert.h>

// archive library
#include "bitextractor.hpp"
#include "bitexception.hpp"
using namespace bit7z;

#define MYPATHLEN 270

void logit(const char*, char*);
void logit(const wchar_t* msg1, wchar_t* msg2);

#define TEMPDIR "E:\\tmp"
#define TEMPDIRW L"E:\\tmp"

extern "C" {
		extern int ph_dct_imagehashW2(wchar_t *filename, CImg<float> *, CImg<float> *, unsigned long long &hash, unsigned long &crc);
		int ph_hamming_distance(unsigned long long hash1, unsigned long long hash2);
		extern void ph_startup();
		extern void ph_shutdown();
		extern CImg<float> *ph_dct_matrix(int);
}

CImg<float> *dct_matrix;
CImg<float> dct_transpose;

unsigned long long do_phash(char *filename, unsigned long &crc)
{
	const size_t cSize = strlen(filename) + 1;
	wchar_t *wc = new wchar_t[cSize];
	size_t outSize;
	mbstowcs_s(&outSize, wc, cSize, filename, cSize-1);

	unsigned long long hash = 0;
	int res = ph_dct_imagehashW2(wc, dct_matrix, &dct_transpose, hash, crc);
	if (res == -5)
	{
		// In the case of memory allocation error, try again [operation on another thread might have finished]
		res = ph_dct_imagehashW2(wc, dct_matrix, &dct_transpose, hash, crc);
	}
	if (res < 0)
		return 0;
	return hash;
}

struct entry
{
	char name[MYPATHLEN]; // TODO convert to wchar_t
	unsigned long long hash;
	unsigned long crc;
};

struct entry* alloc_entry()
{
	return (struct entry *)malloc(sizeof(struct entry));
}

struct distEntry
{
	int distance;
	entry *f1;
	entry *f2;
};

bool diff_comp( struct distEntry e1, struct distEntry e2)
{
	return e1.distance < e2.distance;
}

#pragma warning(disable : 4996)

bool hasEnding(std::wstring const &fullString, std::wstring const &ending) 
{
	if (fullString.length() >= ending.length()) 
	{
		// case-insensitive
		std::wstring end = fullString.substr(fullString.length() - ending.length(), ending.length());
		return 0 == _wcsicmp(end.c_str(), ending.c_str());
		//return (0 == fullString.compare(fullString.length() - ending.length(), ending.length(), ending));
	}
	else 
	{
		return false;
	}
}

// locking around fprintf which appears not to be thread safe
std::mutex mtx;

// Process a file. 1. Calculate the phash. 2. Write the hash, and the filestring.
// TODO output the filestring MINUS the base path.
void processFile(std::wstring fpath, const wchar_t *basepath, const wchar_t* zipfile, FILE *fp)
{
	const wchar_t* filepath = fpath.c_str();
	int len = wcslen(TEMPDIRW) + 1; // include trailing slash

	if (wcsstr(filepath, L"phashc") != NULL)
		return;

	unsigned long crc = 0;
	unsigned long long tmpHash = 0;
	try
	{
		std::string npath(fpath.begin(), fpath.end());
		tmpHash = do_phash((char *)(npath.c_str()), crc);
		if (tmpHash <= 0)
		{
			logit(L"Hash fail", (wchar_t *)filepath);
			return;
		}
	}
	catch (...)
	{
		logit(L"processFile Except:", (wchar_t *)filepath);
		printf("processFile Except: %ls\n", filepath);
		return;
	}

	mtx.lock();
	// stripping the leading temp folder
	fwprintf(fp, L"%llu|%lu|%s|%s\n", tmpHash, crc, zipfile, fpath.substr(len).c_str());
	mtx.unlock();

}

Bit7zLibrary *lib7z;
BitExtractor *extr7z;
BitExtractor* extrRar;
BitExtractor* extrZip;

// Process a directory tree: for each image file, calculate the phash,
// and write the hash value and file string to the output file.
void processTree(const wchar_t *path, wchar_t *basepath, const wchar_t* zipfile, FILE *fp)
{
	std::list<std::wstring> folders;
	std::vector<std::wstring> files;

	wchar_t thispath[514];
	wchar_t apath[514];

	_TDIR *srcdir;

	printf("%ls\n", zipfile);

	try
	{
		wsprintf(thispath, L"%ls%ls", basepath, path);

		struct _tdirent *dent;
		srcdir = _topendir(thispath);
		if (srcdir == NULL)
			return;

		while ((dent = _treaddir(srcdir)) != NULL)
		{
			struct _stat64i32 st;

			if (dent->d_name[0] == '.')
				continue;

			if (wcslen(thispath) + wcslen(dent->d_name) > MYPATHLEN)
			{
				logit(L"Path too long:%s", dent->d_name);
				//printf("Path too long:%s\n", dent->d_name);
				continue;
			}

			wsprintf(apath, L"%ls\\%ls", thispath, dent->d_name);
			_wstat(apath, &st);

			if (st.st_mode & _S_IFDIR) // recurse into subdirectories
			{
				wsprintf(apath, L"%ls\\%ls", path, dent->d_name);
				folders.push_back(apath);
				//			processTree(apath, basepath, fp);
			}
			else
			{
				files.push_back(apath);
				//			processFile(apath, basepath, fp);
			}
		}
		_tclosedir(srcdir);
	}
	catch (...)
	{
		wprintf(L"processtree readdir exception %ls\n", thispath);
		if (srcdir != NULL)
			_tclosedir(srcdir);
	}

	int max = files.size();
	char buff[10];
	logit("File count:", itoa(max,buff,10));
	if (max != 0)
	{
//#pragma omp parallel for
		for (int dex = 0; dex < max; dex++)
		{
			processFile(files[dex].c_str(), TEMPDIRW, zipfile, fp);
		}
	}
	for (std::list<std::wstring>::iterator it = folders.begin(); it != folders.end(); ++it)
	{
		processTree((*it).c_str(), basepath, zipfile, fp);
	}
}

void deleteDirectoryContents(const std::string& dir_path)
{
	for (const auto& entry : fs::directory_iterator(dir_path))
		fs::remove_all(entry.path());
}

void processArchives(std::vector<std::wstring> files, FILE *fp)
{
	fs::create_directory(TEMPDIR);
	for (int i = 0; i < files.size(); i++)  // each archive
	{
		fs::path archpath(files[i]);

		logit(L"PA:", (wchar_t*)files[i].c_str());

		// extract files to folder of form E:\tmp\<zipfile>
		wchar_t outpathW[1024];
		wsprintf(outpathW, L"%ls\\%ls", TEMPDIRW, archpath.stem().c_str());
		fs::create_directory(outpathW);

		//std::string op = outpath;
		//std::wstring outpathW(op.begin(), op.end());

		try
		{
			std::wstring ePath(files[i].begin(), files[i].end());
			if (hasEnding(files[i], L".RAR") || hasEnding(files[i], L".CBR"))
				extrRar->extract(ePath, outpathW);
			if (hasEnding(files[i], L".ZIP") || hasEnding(files[i], L".CBZ"))
				extrZip->extract(ePath, outpathW);
			if (hasEnding(files[i], L".7Z") || hasEnding(files[i], L".CB7"))
				extr7z->extract(ePath, outpathW);

			// process all image files in the archive
			//processTree(outpathW.c_str(), (wchar_t*)L"", files[i].c_str(), fp);
			//processTree(NULL, (wchar_t *)outpathW.c_str(), files[i].c_str(), fp);
			processTree(NULL, outpathW, files[i].c_str(), fp);
		}
		catch (const BitException& be)
		{
			logit(L"Failed to extract from :", (wchar_t *)files[i].c_str());
		}

		fflush(fp);

		// 20110215 delete failing when GIF file encountered (locked?)
		try
		{
			deleteDirectoryContents(outpathW);
			// TODO delete directory outpath
			deleteDirectoryContents((wchar_t*)TEMPDIRW);
		}
		catch (...) {}
	}
}

#define ENDCOUNT 6
const wchar_t* endings[ENDCOUNT] = { L".RAR", L".ZIP", L".7Z", L".CBR", L".CBZ", L".CB7" };

// Process a directory tree: for each archive file:
// 1. extract the contents to an output folder
// 2. run processTree() on the output folder
// 3. delete the output folder
void processTree1(const wchar_t* path, wchar_t *basepath, FILE* fp)
{
	std::vector<std::wstring> folders;
	std::vector<std::wstring> files;

	wchar_t thispath[514];
	wchar_t apath[514];

	_TDIR* srcdir;
	try
	{
		wsprintf(thispath, L"%s%s", basepath, path);

		// better to use feedback per-archive
		//printf("%s\n", thispath); // show progress

		struct _tdirent* dent;
		srcdir = _topendir(thispath);
		if (srcdir == NULL)
			return;

		while ((dent = _treaddir(srcdir)) != NULL)
		{
			struct _stat64i32 st;

			if (dent->d_name[0] == '.')
				continue;

			if (wcslen(thispath) + wcslen(dent->d_name) > MYPATHLEN)
			{
				logit(L"processTree1: Path too long:%s", dent->d_name);
				wprintf(L"Path too long:%ls\n", dent->d_name);
				continue;
			}

			// TODO do we lose unicode chars in either the path or the archive name?
			wsprintf(apath, L"%ls\\%ls", thispath, dent->d_name);
			_wstat(apath, &st);

			if (st.st_mode & _S_IFDIR) // recurse into subdirectories
			{
				wsprintf(apath, L"%ls\\%ls", path, dent->d_name);
				//processTree1(apath, basepath, fp);
				folders.push_back(apath);
			}
			else
			{
				// look for supported extensions
				for (int i =0; i < ENDCOUNT; i++)
					if (hasEnding(apath, endings[i]))
					{
						files.push_back(apath);
						break;
					}
			}
		}
		_tclosedir(srcdir);
		srcdir = NULL;
	}
	catch (...)
	{
		logit(L"readdir exception %ls\n", thispath);
		if (srcdir != NULL)
			_tclosedir(srcdir);
	}

	// testing
	//printf("%s: %d archives found\n", thispath, (int)files.size());
	processArchives(files, fp);

	for (int i = 0; i < folders.size(); i++)
	{
		processTree1(folders[i].c_str(), basepath, fp);
	}
}

void startup()
{
	//omp_set_num_threads(2);
	ph_startup();
	dct_matrix = ph_dct_matrix(32);
	dct_transpose = dct_matrix->get_transpose();

	lib7z = new Bit7zLibrary(L"7z.dll");
	extr7z = new BitExtractor(*lib7z, BitFormat::SevenZip);
	extrZip = new BitExtractor(*lib7z, BitFormat::Zip);
	extrRar = new BitExtractor(*lib7z, BitFormat::Rar);
}

void shutdown() 
{ 
	ph_shutdown(); 
	delete dct_matrix;
}

bool exists(const std::string& name) 
{
	struct stat buffer;
	if (stat(name.c_str(), &buffer) != 0)
	{
		// stat() on Windows can't handle trailing slashes
		printf("ERROR: path <%s> doesn't exist! [did you accidentally include a trailing slash?]\n", name.c_str());
		return false;
	}
	if ( buffer.st_mode & S_IFREG || !(buffer.st_mode & S_IFDIR))
	{
		printf("ERROR: path <%s> is not a folder!\n", name.c_str());
		return false;
	}
	return true;
}

void doit(char *filename)
{
	startup();

	FILE *fp = NULL;

	__try
	{
		const size_t cSize = strlen(filename) + 1;
		wchar_t* wc = new wchar_t[cSize];
		mbstowcs(wc, filename, cSize);

		char filepath[257];
		sprintf(filepath, "%s\\pixel.phashcz", filename);

		fopen_s(&fp, filepath, "w+, ccs=UTF-8");
		if (fp != 0)
		{
			fwprintf(fp, L"%ls\n", wc);
			processTree1(L"", wc, fp);
			fclose(fp);
			fp = 0;
		}
	}
	__finally
	{
		shutdown();
		if (fp != 0)
		{
			fflush(fp);
			fclose(fp);
		}
	}
}

char initial_path[MYPATHLEN];

void logit(const char *msg1, char *msg2)
{
	char logpath[MYPATHLEN+30];
	strcpy(logpath, initial_path);
	strcat(logpath, "\\hasherz.log");

	FILE *fp = NULL;
	fopen_s(&fp, logpath, "a+");
	if (fp == 0)
		return;
	fprintf(fp, "%s:%s\n", msg1, msg2);
	fflush(fp);
	fclose(fp);
}

void logit(const wchar_t* msg1, wchar_t* msg2)
{
	char logpath[MYPATHLEN + 30];
	strcpy(logpath, initial_path);
	strcat(logpath, "\\hasherz.log");

	FILE* fp = NULL;
	fopen_s(&fp, logpath, "a+");
	if (fp == 0)
		return;
	fwprintf(fp, L"%ls:%ls\n", msg1, msg2);
	fflush(fp);
	fclose(fp);
}

int main(int argc, char *argv[])
{
	setlocale(LC_ALL, "");
	if (argc < 2)
	{
		printf("SYNTAX: %s <folder path>\n", argv[0]);
		return 1;
	}

	if (!exists(argv[1]))
	{
		printf("PATH doesn't exist! >%s<\n", argv[1]);
		return 1;
	}

	char *p = strrchr(argv[0], '\\'); // NOTE: assuming windows!
	if (p)
	{
		p[0] = 0;
		strcpy(initial_path, argv[0]);
	}
	else
	{
		char temp[MYPATHLEN];

		if (_getcwd(temp, MYPATHLEN) != 0)
			strcpy(initial_path, temp);
	}
	printf("%s\n", initial_path);

	doit(argv[1]);

	return 1;
}

