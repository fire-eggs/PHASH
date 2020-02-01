// TestApp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <vector>
#include <algorithm>

#include "dirent.h"
#include <sys/stat.h>
#include "direct.h"

#include "omp.h"
#include "CImg.h"
using namespace cimg_library;

#include <string>
#include <list>
#include <assert.h>

void logit(char*, char*);

extern "C" {
		extern int ph_dct_imagehash(const char* file, unsigned long long &hash);
		extern int ph_dct_imagehashW(wchar_t *filename, unsigned long long &hash, unsigned long &crc);
		extern int ph_dct_imagehashW2(wchar_t *filename, CImg<float> *, CImg<float> *, unsigned long long &hash, unsigned long &crc);
		extern int gibber(const wchar_t *filename, unsigned long long &hash, unsigned long &crc);
		int ph_hamming_distance(unsigned long long hash1, unsigned long long hash2);
		extern void ph_startup();
		extern void ph_shutdown();
		extern CImg<float> *ph_dct_matrix(int);
}

CImg<float> *dct_matrix;
CImg<float> dct_transpose;

//unsigned long long do_hash(char *filename)
//{
//	unsigned long long hash;
//	if (ph_dct_imagehash(filename, hash) < 0)
//	{
//		printf("***Fail %s\n", filename);
//		return -1;
//	}

//	return hash;
//}

unsigned long long do_dhash(char *filename, unsigned long &crc)
{
	const size_t cSize = strlen(filename) + 1;
	wchar_t *wc = new wchar_t[cSize];
	size_t outSize;
	mbstowcs_s(&outSize, wc, cSize, filename, cSize - 1);

	unsigned long long hash;
	if (gibber(wc, hash, crc) < 0)
		return 0;
	return hash;
}

unsigned long long do_phash(char *filename, unsigned long &crc)
{
	const size_t cSize = strlen(filename) + 1;
	wchar_t *wc = new wchar_t[cSize];
	size_t outSize;
	mbstowcs_s(&outSize, wc, cSize, filename, cSize-1);

	unsigned long long hash;
	//if (ph_dct_imagehashW(wc, hash, crc) < 0)
	//	return 0;

	if (ph_dct_imagehashW2(wc, dct_matrix, &dct_transpose, hash, crc) < 0)
		return 0;
	return hash;
}

struct entry
{
	char name[256]; // TODO convert to wchar_t
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

bool hasEnding(std::string const &fullString, std::string const &ending) 
{
	if (fullString.length() >= ending.length()) 
	{
		return (0 == fullString.compare(fullString.length() - ending.length(), ending.length(), ending));
	}
	else 
	{
		return false;
	}
}

// Process a file. 1. Calculate the phash. 2. Write the hash, and
// the filestring.
// TODO output the filestring MINUS the base path.
void processFile(char *filepath, char *basepath, FILE *fp)
{
	if (strstr(filepath, "phashc") != NULL)
		return;

	try
	{
		unsigned long crc;
		unsigned long long tmpHash = do_phash(filepath, crc);
//		unsigned long long tmpHash = do_dhash(filepath, crc);
		if (tmpHash <= 0)
		{
			logit("Hash fail", filepath);
			return;
		}
		fprintf(fp, "%llu|%lu|%s\n", tmpHash, crc, filepath);
	}
	catch (...)
	{
		printf("processFile Except: %s", filepath);
		//logit("processFile Exception", filepath);
		//throw;
	}
}


// Process a directory tree: for each jpg file, calculate the phash,
// and write the hash value and file string to the output file.
void processTree(const char *path, char *basepath, FILE *fp)
{
	std::list<std::string> folders;
	std::vector<std::string> files;

	char thispath[514];
	char apath[514];

	DIR *srcdir;
	try
	{
		sprintf(thispath, "%s%s", basepath, path);
		printf("%s\n", thispath);

		struct dirent *dent;
		srcdir = opendir(thispath);
		if (srcdir == NULL)
			return;

		while ((dent = readdir(srcdir)) != NULL)
		{
			struct stat st;

			if (dent->d_name[0] == '.')
				continue;

			if (strlen(thispath) + strlen(dent->d_name) > 256)
			{
				printf("Path too long:%s", dent->d_name);
				continue;
			}

			sprintf(apath, "%s\\%s", thispath, dent->d_name);
			stat(apath, &st);

			if (st.st_mode & _S_IFDIR) // recurse into subdirectories
			{
				sprintf(apath, "%s\\%s", path, dent->d_name);
				folders.push_back(apath);
				//			processTree(apath, basepath, fp);
			}
			else
			{
				files.push_back(apath);
				//			processFile(apath, basepath, fp);
			}
		}
		closedir(srcdir);
	}
	catch (...)
	{
		printf("readdir except %s", thispath);
		if (srcdir != NULL)
			closedir(srcdir);
	}

	int max = files.size();
	if (max != 0)
	{
// fprintf appears not to be thread-safe 20180618
//#pragma omp parallel for
		for (int dex = 0; dex < max; dex++)
		{
			processFile((char *)(files[dex].c_str()), basepath, fp);
		}
	}
	for (std::list<std::string>::iterator it = folders.begin(); it != folders.end(); ++it)
	{
		processTree((*it).c_str(), basepath, fp);
	}
}

void startup() 
{ 
//	omp_set_num_threads(2);
	ph_startup(); 
	dct_matrix = ph_dct_matrix(32);
	dct_transpose = dct_matrix->get_transpose();
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
	printf("DI1");
	
	startup();
	
	printf("DI2");

	FILE *fp = NULL;

	__try
	{
		char filepath[257];
		//sprintf(filepath, "%s\\pixel.phashd", filename);
		sprintf(filepath, "%s\\pixelLAA.phashc", filename);

		printf("DI2A -");
		printf(filepath);
		printf("\n");
		
		//fopen_s(&fp, filepath, "w+");
		fp = fopen(filepath, "w+");
		printf("DI2A1\n");
		fprintf(fp, "%s\n", filename);

		printf("DI2B");
		
		processTree("", filename, fp);
		fclose(fp);
		
		printf("DI2C");
	}
	__finally
	{
		shutdown();
		if (fp != NULL)
		{
			fflush(fp);
			fclose(fp);
		}
	}
	printf("DI3");
}

char initial_path[260];

void logit(char *msg1, char *msg2)
{
	char logpath[300];
	strcpy(logpath, initial_path);
	strcat(logpath, "\\testappLAA.log");

	FILE *fp = NULL;
	fopen_s(&fp, logpath, "a+");
	fprintf(fp, "%s:%s\n", msg1, msg2);
	fflush(fp);
	fclose(fp);
}

int main(int argc, char *argv[])
{
	if (argc < 2)
	{
		printf("SYNTAX: %s <folder path>\n", argv[0]);
		return 1;
	}

	if (!exists(argv[1]))
	{
		printf("Folder %s doesn't exist!", argv[1]);
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
		char temp[260];

		if (_getcwd(temp, 260) != 0)
			strcpy(initial_path, temp);
	}
	printf("%s\n", initial_path);

	doit(argv[1]);

	return 1;
}

