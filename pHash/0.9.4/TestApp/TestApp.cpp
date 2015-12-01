// TestApp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <vector>
#include <algorithm>

#include "dirent.h"
#include <sys/stat.h>


extern "C" {
	// TODO header
	// Access to the phash library methods
		extern int ph_dct_imagehash(const char* file, unsigned long long &hash);
		extern int ph_dct_imagehashW(wchar_t *filename, unsigned long long &hash);
		int ph_hamming_distance(unsigned long long hash1, unsigned long long hash2);
		extern void ph_startup();
		extern void ph_shutdown();
}

//extern "C" int phash_hamming_distance(const unsigned long long hash1, const unsigned long long hash2)
//{
//	unsigned long long x = hash1^hash2;
//	const unsigned long long m1 = 0x5555555555555555ULL;
//	const unsigned long long m2 = 0x3333333333333333ULL;
//	const unsigned long long h01 = 0x0101010101010101ULL;
//	const unsigned long long m4 = 0x0f0f0f0f0f0f0f0fULL;
//	x -= (x >> 1) & m1;
//	x = (x & m2) + ((x >> 2) & m2);
//	x = (x + (x >> 4)) & m4;
//	return (x * h01) >> 56;
//}

unsigned long long do_hash(char *filename)
{
//	printf("Hashing: %s\n", filename);
	unsigned long long hash;
	if (ph_dct_imagehash(filename, hash) < 0)
	{
		printf("***Fail %s\n", filename);
		return -1;
	}

//	printf("Hash: %llx Image: %s\n", hash, filename);
	return hash;
}

unsigned long long do_hash2(char *filename)
{
	const size_t cSize = strlen(filename) + 1;
	wchar_t *wc = new wchar_t[cSize];
	//std::wstring wc(cSize, L'#');
	//mbstowcs(&wc[0], filename, cSize);
	size_t outSize;
	mbstowcs_s(&outSize, wc, cSize, filename, cSize-1);

	unsigned long long hash;
	if (ph_dct_imagehashW(wc, hash) < 0)
		return 0;
	return hash;
}

struct entry
{
	char name[256]; // TODO convert to wchar_t
	unsigned long long hash;
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
	unsigned long long tmpHash = do_hash2(filepath);
	if (tmpHash <= 0)
		return;
	fprintf(fp, "%llu|%s\n", tmpHash, filepath);

	if (false)
	{
		if (!hasEnding(filepath, ".jpg"))
			return; // jpeg only

		unsigned long long tmpHash = do_hash(filepath);
		if (tmpHash < 0)
			return; // failure?

		unsigned long long tmpHash2 = do_hash2(filepath); // TODO need to get original filename as wchar_t

		// TODO tmpHash and tmpHash2 must match
		if (tmpHash != tmpHash2)
		{
			__debugbreak();
		}

		fprintf(fp, "%llu|%s\n", tmpHash, filepath);
	}
}

// Process a directory tree: for each jpg file, calculate the phash,
// and write the hash value and file string to the output file.
void processTree(char *path, char *basepath, FILE *fp)
{
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
			processTree(apath, basepath, fp);
		}
		else
		{
			processFile(apath, basepath, fp);
		}
	}
	closedir(srcdir);
}

void startup() { ph_startup(); }
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
		sprintf(filepath, "%s\\trial.phash", filename);

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

#if 0
int main(int argc, char* argv[])
{
	std::vector<entry> foo;
	std::vector<distEntry> diffs;

	//struct entry foo[500];
	int eDx = 0;

	if (argc > 1) 
	{
		DIR *pDir = opendir(argv[1]);
		if (pDir == NULL)
		{
			for (int i = 1; i < argc; i++)
			{
				do_hash(argv[i]);
			}
		}
		else
		{
			char path[256];

			struct dirent *pDirent;
			while ((pDirent = readdir(pDir)) != NULL)
			{
				strcpy_s(path, argv[1]);
				strcat_s(path, "\\");
				strcat_s(path, pDirent->d_name);
				if (pDirent->d_name[0] != '.')
				{
					unsigned long long tmpHash = do_hash(path);
					if (tmpHash < 0)
						continue;

					entry *aFoo = alloc_entry();   // TODO memory leak?
					aFoo->hash = tmpHash;
					strcpy_s(aFoo->name, pDirent->d_name);
					foo.push_back(*aFoo);

					//foo[eDx].hash = do_hash(path);
					//if (foo[eDx].hash != -1)
					//{
					//	strcpy_s(foo[eDx].name, pDirent->d_name);
					//	eDx++;
					//}
				}
			}
		}
		closedir(pDir);

		for (unsigned int i = 0; i < foo.size(); i++)
		{
			for (unsigned int j = i + 1; j < foo.size(); j++)
			{
				int d = ph_hamming_distance(foo[i].hash, foo[j].hash);
				if (d < 25)
				{
					distEntry adiff;
					adiff.distance = d;
					adiff.f1 = &foo[i];
					adiff.f2 = &foo[j];
					diffs.push_back(adiff);
				}
			}
		}

		std::sort(diffs.begin(), diffs.end(), diff_comp);

		FILE *fp;
		fopen_s(&fp, "E:\\testaugust.phash", "w+");
		fprintf(fp, "%s\n", argv[1]);
		for (unsigned int i = 0; i < diffs.size(); i++)
		{
			fprintf(fp, "%d : %s : %s\n", diffs[i].distance, diffs[i].f1->name, diffs[i].f2->name);
		}
		fclose(fp);

		//for (unsigned int i = 0; i < diffs.size(); i++)
		//{
		//	printf("%d : %s : %s\n", diffs[i].distance, diffs[i].f1->name, diffs[i].f2->name);
		//}

//		for (int i = 0; i < eDx - 1; i++)
//		{
///*			printf("\n");*/
//			for (int j = i + 1; j < eDx; j++)
//			{
//				int d = ph_hamming_distance(foo[i].hash, foo[j].hash);
//				if (d < 15)
//					printf("%d : %s : %s\n", d, foo[i].name, foo[j].name);
//			}
//		}
	}
	else 
	{
		// no commandline
		printf("SYNTAX: %s image_file\n", argv[0]);
		return 1;
	}
}
#endif

