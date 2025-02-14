#include <vector>
#include <string>
#include <iostream>

void CreateDatapack(const std::vector<std::string>& args);
void DatapackExtractor(const std::vector<std::string>& args);

void main(int argc, char* argv[]) {
    std::vector<std::string> args;
    for (int i = 0; i < argc; ++i)
        args.emplace_back(argv[i]);

    // datapack creator
    if (std::find(args.begin(), args.end(), "/create") != args.end()) {
        args.erase(std::find(args.begin(), args.end(), "/create"));
        CreateDatapack(args);
        return;
    }

    // datapack extractor
    if (std::find(args.begin(), args.end(), "/extract") != args.end()) {
        args.erase(std::find(args.begin(), args.end(), "/extract"));
        DatapackExtractor(args);
        return;
    }
}