"""
Reads all csv files in a directory and plots line plots and save them as pdfs
   in a new subdiretory of the provided directory.
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
from matplotlib import pyplot as plt
import matplotlib.backends.backend_pdf
import sys
from pathlib import Path
import PIL
import  datetime
from typing import List

plotHyperbolas = True
original_is_metric = True

def adjustment():
    if original_is_metric:
        return 1.0
    return 0.3048

csv_directory = sys.argv[1]

csv_path = Path(csv_directory)
pdf_path = Path(csv_directory + '\\pdf')
pdf_path.mkdir(parents=True, exist_ok=True)

plt.style.use('ggplot')

x: Path
csv_files = [x for x in csv_path.iterdir() if x.is_file() and x.suffix == '.csv']

class paramStuffs:
    def __init__(self, file_name: Path):
        file_to_open = file_name.with_suffix('.txt')
        with open(file_to_open) as a_file:
            self.all_lines = [line.rstrip() for line in a_file]

    def _split_multiply_merge(self, text_list: List[str], multiplicand: float):
        ret_list = list()
        for a_str in text_list:
            lt, rt = a_str.split('=')
            if not '%' in a_str:
                rt = float(rt) * multiplicand
                a_str = f'{lt} = {rt:.2f}'
            else:
                lt = lt.replace('a', '')
                a_str = f'{lt} = {rt}'
            ret_list.append(a_str)
        return ret_list

    def get_formatted_left_text(self):
        start_idx = 4
        lines = self.all_lines[start_idx:start_idx+4]
        lines = self._split_multiply_merge(lines, adjustment())

        return f'{lines[0]}\n{lines[1]}\n{lines[2]}\n{lines[3]}\n'

    def get_formatted_right_text(self):
        start_idx = 0
        lines = self.all_lines[start_idx:start_idx+4]
        lines = self._split_multiply_merge(lines, adjustment())

        return f'{lines[0]}\n{lines[1]}\n{lines[2]}\n{lines[3]}\n'

# from https://gist.githubusercontent.com/thriveth/8560036/raw/3039007a6a1ce06325a62de108a2b2351d2eab6d/CBcolors.py
color_blind_cycle = ['#377eb8', '#ff7f00', '#4daf4a',
                  '#f781bf', '#a65628', '#984ea3',
                  '#999999', '#e41a1c', '#dede00']
# 0 blue; 1 orange; 2 olive green; 3 purple pink;; 4 deep red; 5 purple; 6 gray; 7 matte red

existing_ground_color = color_blind_cycle[6]
good_fit_hyperbola_color = color_blind_cycle[4]

# outfile = str(pdf_path.resolve()) + '\\' + 'hyperbola plots original ground.pdf'
outfile = str(pdf_path.resolve()) + '\\' + 'hyperbola plots.pdf'
pdf = matplotlib.backends.backend_pdf.PdfPages(outfile)
for a_file in csv_files:
    params = paramStuffs(a_file)

    # Set the figure size
    plt.rcParams["figure.figsize"] = [7.00, 3.50]
    plt.rcParams["figure.autolayout"] = True
    fig, ax = plt.subplots(figsize=(7.0, 3.5))
    ax.set_ylabel('Elevation (m)')
    ax.set_xlabel('Offset (m)')

    # Make a list of columns
    # columns = ['mpg', 'displ', 'hp', 'weight']
    columns = "station,elevation,vcLength".split(",")
    with open(a_file) as file_handle:
        columns = file_handle.readline().rstrip().split(",")
    columnsToUse = [x for x in columns if x != "vcLength"]
    if plotHyperbolas == True:
        columnsToUse = ['station', 'elevation', 'hyperbolaValue']
    else:
        columnsToUse = ['station', 'elevation'] #, 'hyperbolaValue']

    # Read a CSV file
    df = pd.read_csv(a_file, header=0, names=columns, usecols=columnsToUse, index_col='station')
    df.index *= adjustment()
    df['elevation'] *= adjustment()
    if plotHyperbolas == True:
        df['hyperbolaValue'] *= adjustment()

    # Plot the lines
    df_elevation = df["elevation"]
    df_elevation.plot.line(linewidth=2, color=existing_ground_color, linestyle='dashed', y=["elevation"])
    if plotHyperbolas == True:
        df_hyperbola = df["hyperbolaValue"]
        df_hyperbola.plot.line(linewidth=1, color=good_fit_hyperbola_color, linestyle='solid')

    plt.xlabel("Offset (m)")
    plt.legend()
    title = Path(a_file).stem[:-4]
    plt.title(title)
    plt.rcParams['axes.facecolor'] = 'white'
    plt.grid(visible=True, which='major', color='k', linestyle='-', zorder=2)
    plt.grid(visible=True, which='minor', color='silver', linestyle='-', linewidth=0.2, zorder=1, alpha=0.9)
    plt.minorticks_on()
    plt.subplots_adjust(bottom=-4.35)
    left_text = params.get_formatted_left_text()
    right_text = params.get_formatted_right_text()
    plt.figtext(0.1, 0.01, left_text, horizontalalignment='left',
                verticalalignment='bottom', fontsize=12)
    plt.figtext(0.7, 0.01, right_text, horizontalalignment='left',
                verticalalignment='bottom', fontsize=12)
    # ax.patch.set_edgecolor('black')
    # ax.patch.set_linewidth(5)
    # ax.patch.set(zorder=0)

    plt.gca().set_aspect('equal')
    fig = matplotlib.pyplot.gcf()
    scale = 100.0
    bottomMarg = 1.6
    pageHeight = fig.bbox.y1 / scale
    pageHeight += bottomMarg
    fig.set_size_inches(7.5, pageHeight)

    # fig.set_size_inches(7.5, 10.5)


    # plot = df.plot(linewidth=2, color="dimgrey", linestyle='dashed', title=a_file.stem)
    # color names at https://matplotlib.org/stable/gallery/color/named_colors.html
    # color blind palettes at https://yoshke.org/blog/colorblind-friendly-diagrams

    # fig = df.plot.get_figure()
    # pdf_file_name = a_file.name[:-4] + '.pdf'
    # new_file_name = str(pdf_path.resolve()) + '\\' + pdf_file_name
    # plt.savefig(new_file_name, format="pdf") #, bbox_inches="tight")
    # print(pdf_file_name)
    pdf.savefig()

    # plt.show()

    dbg = 1


pdf.close()

print(f'created {outfile}')
print(f'at {datetime.datetime.now().strftime("%H:%M")}')
print()
print()


