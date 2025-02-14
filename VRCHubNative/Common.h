#pragma once
#include <string>
#include <thread>

#define SleepFor(x) std::this_thread::sleep_for(std::chrono::milliseconds(x));
std::string CreateTempFolder();
void ZipFolder(const std::string& folderPath, const std::string& outputFilePath);
void UnzipFolder(const std::string& ZipFile, const std::string& OutputFolder);
class PackageJson {
public:
    std::string worldName;
    std::string worldHash;
    std::string version;
    std::string author;
    std::string discord;
    PackageJson(const std::string& wn, const std::string& wh, const std::string& v, const std::string& a, const std::string& d)
        : worldName(wn), worldHash(wh), version(v), author(a), discord(d) {
    }
};
