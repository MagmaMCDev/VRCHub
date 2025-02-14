#include <string>
#include <random>
#include <sstream>
#include <filesystem>

std::string CreateTempFolder() {
    std::string tempPath = std::filesystem::temp_directory_path().string();
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<> dis(10000, 99999);
    std::string randomDir = tempPath + "\\temp_pack_" + std::to_string(dis(gen));

    std::filesystem::create_directory(randomDir);
    return randomDir;
}
void ZipFolder(const std::string& folderPath, const std::string& outputFilePath) {
    std::string zipFilePath = outputFilePath + ".zip";
    std::stringstream command;
    command << "powershell -NoProfile -NoLogo -NonInteractive Compress-Archive -Path '" << folderPath << "\\*' -DestinationPath '" << zipFilePath << "'";
    system(command.str().c_str());
    std::filesystem::rename(zipFilePath, outputFilePath);
}
void UnzipFolder(const std::string& ZipFile, const std::string& OutputFolder) {
    std::stringstream command;
    command << "powershell -NoProfile -NoLogo -NonInteractive Expand-Archive -LiteralPath '" << ZipFile << "' -DestinationPath '" << OutputFolder << "'";
    system(command.str().c_str());
}