#include <fstream>
#include <sstream>
#include <string>
#include <filesystem>
#include <thread>
#include "Common.h"
#include <iostream>

PackageJson ReadPackageJson(const std::string& folderPath) {
    std::ifstream jsonFile(folderPath + "\\package.json");
    if (!jsonFile) {
        std::cerr << "Failed to open package.json in " << folderPath << std::endl;
        return PackageJson("", "", "", "", "");
    }

    std::stringstream buffer;
    buffer << jsonFile.rdbuf();
    std::string jsonContent = buffer.str();

    size_t pos;
    std::string worldName, worldHash, version, author, discord;

    pos = jsonContent.find("\"WorldName\": ");
    if (pos != std::string::npos) {
        size_t start = jsonContent.find("\"", pos + 13) + 1;
        size_t end = jsonContent.find("\"", start);
        worldName = jsonContent.substr(start, end - start);
    }

    pos = jsonContent.find("\"WorldHash\": ");
    if (pos != std::string::npos) {
        size_t start = jsonContent.find("\"", pos + 13) + 1;
        size_t end = jsonContent.find("\"", start);
        worldHash = jsonContent.substr(start, end - start);
    }

    pos = jsonContent.find("\"Version\": ");
    if (pos != std::string::npos) {
        size_t start = jsonContent.find("\"", pos + 11) + 1;
        size_t end = jsonContent.find("\"", start);
        version = jsonContent.substr(start, end - start);
    }

    pos = jsonContent.find("\"Author\": ");
    if (pos != std::string::npos) {
        size_t start = jsonContent.find("\"", pos + 9) + 1;
        size_t end = jsonContent.find("\"", start);
        author = jsonContent.substr(start, end - start);
    }

    pos = jsonContent.find("\"Discord\": ");
    if (pos != std::string::npos) {
        size_t start = jsonContent.find("\"", pos + 11) + 1;
        size_t end = jsonContent.find("\"", start);
        discord = jsonContent.substr(start, end - start);
    }

    jsonFile.close();
    return PackageJson(worldName, worldHash, version, author, discord);
}

void DatapackExtractor(const std::vector<std::string>& args) {
    if (args.size() != 3) {
        std::cerr << "Usage: DatapackExtractor <input> <output>" << std::endl;
        SleepFor(500);
        return;
    }

    std::string inputFilePath = args[1];
    std::string outputFilePath = args[2];

    std::filesystem::rename(inputFilePath, inputFilePath + ".zip");
    UnzipFolder(inputFilePath + ".zip", outputFilePath);
    std::filesystem::rename(inputFilePath + ".zip", inputFilePath);
}
