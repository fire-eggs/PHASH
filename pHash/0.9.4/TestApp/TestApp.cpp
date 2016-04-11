// TestApp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <vector>
#include <algorithm>

#include "dirent.h"
#include <sys/stat.h>

#include "omp.h"

extern "C" {
		extern int ph_dct_imagehash(const char* file, unsigned long long &hash);
		extern int ph_dct_imagehashW(wchar_t *filename, unsigned long long &hash, unsigned long &crc);
		int ph_hamming_distance(unsigned long long hash1, unsigned long long hash2);
		extern void ph_startup();
		extern void ph_shutdown();
}

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

unsigned long long do_hash2(char *filename, unsigned long &crc)
{
	const size_t cSize = strlen(filename) + 1;
	wchar_t *wc = new wchar_t[cSize];
	//std::wstring wc(cSize, L'#');
	//mbstowcs(&wc[0], filename, cSize);
	size_t outSize;
	mbstowcs_s(&outSize, wc, cSize, filename, cSize-1);

	unsigned long long hash;
	if (ph_dct_imagehashW(wc, hash, crc) < 0)
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
	unsigned long crc;
	unsigned long long tmpHash = do_hash2(filepath, crc);
	if (tmpHash <= 0)
		return;
	fprintf(fp, "%llu|%lu|%s\n", tmpHash, crc, filepath);
}

#include <string>
#include <list>

// Process a directory tree: for each jpg file, calculate the phash,
// and write the hash value and file string to the output file.
void processTree(const char *path, char *basepath, FILE *fp)
{
	std::list<std::string> folders;
	std::vector<std::string> files;

	char thispath[257];
	char apath[257];

	sprintf(thispath, "%s%s", basepath, path);
	struct dirent *dent;
	DIR *srcdir = opendir(thispath);
	if (srcdir == NULL)
		return;

	while ((dent = readdir(srcdir)) != NULL)
	{
		struct stat st;

		if (dent->d_name[0] == '.')
			continue;

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

	int max = files.size();
#pragma omp parallel for
	for (int dex = 0; dex < max; dex++)
	{
		processFile((char *)(files[dex].c_str()), basepath, fp);
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
}

void shutdown() { ph_shutdown(); }

bool exists(const std::string& name) 
{
	struct stat buffer;
	return (stat(name.c_str(), &buffer) == 0);
}

void doit(char *filename)
{
	startup();

	__try
	{
		char filepath[257];
		sprintf(filepath, "%s\\gdi_trial.phashc", filename);

		FILE *fp;
		fopen_s(&fp, filepath, "w+");
		fprintf(fp, "%s\n", filename);

		processTree("", filename, fp);
		fclose(fp);
	}
	__finally
	{
		shutdown();
	}
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
		printf("ERROR: path <%s> doesn't exist!\n", argv[1]);
		return 1;
	}

	doit(argv[1]);

	return 1;
}

