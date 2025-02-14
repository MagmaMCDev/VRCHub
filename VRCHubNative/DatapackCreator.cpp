#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <regex>
#include <vector>
#include <filesystem>
#include "Common.h"

bool ReplaceWorldID(const std::string& inputFilePath, const std::string& outputFilePath, const std::string& newWorldID) {
    std::ifstream inputFile(inputFilePath, std::ios::binary);
    if (!inputFile) {
        std::cerr << "Failed to open input file: " << inputFilePath << std::endl;
        return false;
    }

    std::vector<char> fileData((std::istreambuf_iterator<char>(inputFile)), std::istreambuf_iterator<char>());
    inputFile.close();

    std::string fileContent(fileData.begin(), fileData.end());

    std::regex wrldRegex("wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
    fileContent = std::regex_replace(fileContent, wrldRegex, newWorldID);

    std::vector<char> newFileData(fileContent.begin(), fileContent.end());

    std::ofstream outputFile(outputFilePath, std::ios::binary);
    if (!outputFile) {
        std::cerr << "Failed to create output file: " << outputFilePath << std::endl;
        return false;
    }

    outputFile.write(newFileData.data(), newFileData.size());
    outputFile.close();
    return true;
}
void CreatePackageJson(const std::string& folderPath, const std::string& worldName, const std::string& worldHash, const std::string& version, const std::string& author) {
    std::ofstream jsonFile(folderPath + "\\package.json");
    if (!jsonFile) {
        std::cerr << "Failed to create package.json in " << folderPath << std::endl;
        return;
    }

    jsonFile << "{\n";
    jsonFile << "    \"WorldName\": \"" << worldName << "\",\n";
    jsonFile << "    \"WorldHash\": \"" << worldHash << "\",\n";
    jsonFile << "    \"Version\": \"" << version << "\",\n";
    jsonFile << "    \"Author\": \"" << author << "\",\n";
    jsonFile << "    \"Discord\": \"discord.vrchub.site\"\n";
    jsonFile << "}\n";

    jsonFile.close();
}


void CreateDatapack(const std::vector<std::string>& args) {
    if (args.size() != 8) {
        std::cerr << "Usage: DatapackCreator <input> <output> <WorldID> <WorldName> <WorldHash> <Version> <Author>" << std::endl;
        SleepFor(500);
        return;
    }

    std::string inputFilePath = args[1];
    std::string outputFilePath = args[2];
    std::string newWorldID = args[3];
    std::string worldName = args[4];
    std::string worldHash = args[5];
    std::string version = args[6];
    std::string author = args[7];

    std::string tempFolder = CreateTempFolder();

    std::string dataFilePath = tempFolder + "\\__data";
    if (!ReplaceWorldID(inputFilePath, dataFilePath, newWorldID)) {
        std::filesystem::remove_all(tempFolder);
        return;
    }

    CreatePackageJson(tempFolder, worldName, worldHash, version, author);
    ZipFolder(tempFolder, outputFilePath);
    std::filesystem::remove_all(tempFolder);
    std::cout << "Datapack created at: " << outputFilePath << std::endl;
}
