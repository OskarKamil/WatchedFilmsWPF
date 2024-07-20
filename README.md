# WatchedFilmsWPF

## About

Watched Films Tracker is a comprehensive application designed to help film enthusiasts track and manage their watched films. With a user-friendly interface, it allows users to record, organize, and search through their film collection with ease. Whether you're keeping track of films you've watched, planning to watch, or compiling detailed statistics about your viewing habits, Watched Films Tracker is the perfect tool for any movie lover.

## Features

- **Film Information:** Store comprehensive details about each film, including English title, original title, type, release year, personal rating, watch date, and comments.
- **CSV File Storage:** Data is stored in a CSV file, ensuring easy access and portability.
- **Data Visualization:** View film records in an intuitive table format.
- **Statistics:** Access simple statistics such as average rating per film and films watched per day or week.
- **Sorting:** Sort films by title, release year, watch date, personal rating, or comments for better organization.
- **Search:** Utilize the search bar to effortlessly find a film by title, release year, or any other film detail.
- **Portability:** Store your list of films within the program folder, making the application a portable choice.

## Screenshots

![Main page](https://github.com/OskarKamil/WatchedFilmsWPF/blob/main/External/screenshots%20of%20versions/0.014.png?raw=true)  
*Screenshot 1: Main interface displaying the list of films*

## Requirements

- **Operating System:** Windows
- **Development Environment:** Visual Studio .NET
- **.NET Framework:** The .NET framework is required, which is typically pre-installed on Windows systems.
- **Additional Libraries:** None

## Limitations

Currently, the application only allows reading text and CSV files with the specific number of columns: English title, original title, type, release year, personal rating, watch date, and comments. The application will not work with files that have a different number of columns or a different column order. If the file has more columns, the application will ignore columns not included in the specifications, which will lead to data loss. Work is ongoing to allow more flexibility in the file reading process. In the upcoming releases, the application will be able to read files with any number of columns and any column order, and it will let users edit the columns in the program interface. Users will be able to delete, add, or remove columns from the file.

## Usage 
### 1. Clone Code and Run in Visual Studio

Clone the repository from GitHub:
```
git clone https://github.com/OskarKamil/WatchedFilmsWPF.git
```
- Open the cloned repository in Visual Studio.
- Build and run the project within Visual Studio.

### 2. First Time User: Download and Run the Executable

- Navigate to the Releases section on GitHub.
- Click on the latest release.
- Download the archive file.
- Extract the contents of the archive using a tool like 7-Zip.
- Open the extracted folder.
- Run the executable file (.exe). Note that you may need the .NET redistributable installed, but it is usually included by default on recent Windows systems.

### 3. Updating the Application

- Navigate to the Releases section on GitHub.
- Click on the latest release.
- Download the new executable file (.exe).
- Copy the new executable file into your program directory, replacing the old one.
- Optionally, copy the new ExampleFile.txt and ReadMe.txt files if there are updates to these documents.

## Contributing

Contributions are welcome! If you would like to contribute to this project, please fork the repository and submit a pull request.

## License

This project is open source under the [MIT License](https://opensource.org/licenses/MIT). Feel free to use, modify, and distribute this code as you see fit.
