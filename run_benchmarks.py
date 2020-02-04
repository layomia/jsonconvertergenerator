import json
import os
import random

def clean_state():
	os.system('cmd /c "RMDIR /Q/S bin > nul 2>&1"')
	os.system('cmd /c "RMDIR /Q/S obj > nul 2>&1"')
	os.system('cmd /c "taskkill /F /IM dotnet.exe /T > nul 2>&1"')

processes = ["Deserialize", "Serialize"]
types = ["LoginViewModel", "Location", "IndexViewModel", "MyEventsListerViewModel", "CollectionsOfPrimitives"]
mechanisms = ["Jil", "Json.NET", "Utf8Json", "Default", "AOT_LoadConverters", "AOT_Raw"]

clean_state()

results = {}

def run_new_benchmarks(output_path):
	new_results = {}

	for process in processes:
		for t in types:
			for mechanism in mechanisms:
				job = "{} {} {}".format(process, t, mechanism)
				print "Running {}".format(job)

				elasped_times = []

				for i in range(10):
					elasped_time =  os.popen('cmd /c "dotnet run Benchmarks {} {} {}"'.format(process, mechanism, t)).read()
					print "{} us".format(elasped_time)

					elasped_times.append(int(elasped_time))
					clean_state()

				average = sum(elasped_times) / len(elasped_times)

				new_results[job] = average
				print "Average: {} us".format(average)
				print
	
	with open(output_path, "w") as results_file:
		json.dump(new_results, results_file)
	
	return new_results

def load_results(input_path):
	new_results = {}
	with open(input_path) as results_file:
		new_results = json.load(results_file)

	return new_results


results = load_results('start_up_results_with_other_libs.json')
# results = run_new_benchmarks('start_up_results_with_other_libs.json')
print results

print "Summary\n=======\n"

for process in processes:
	for t in types:
		title = "{} {}".format(process, t)
		print title
		print

		max_average = max([results["{} {}".format(title, mechanism)] for mechanism in mechanisms])

		test_col_values = [len(x) for x in mechanisms]
		test_col_values.append(len("Test"))

		test_col_width = max(test_col_values)
		mean_col_width = max(len("Mean (us)"), len(str(max_average)))
		ratio_col_width = len("Ratio")

		# Write header
		header = "| {} | {} | {} |".format(
			"Test" + "".join([" "] * (test_col_width - len("Test"))),
			"Mean (us)" + "".join([" "] * (mean_col_width - len("Mean (us)"))),
			"Ratio")

		header_underline = "|{}|{}|{}|".format(
			"".join(["-"] * (test_col_width + 2)),
			"".join(["-"] * (mean_col_width + 2)),
			"".join(["-"] * (ratio_col_width + 2)))

		print header
		print header_underline

		for mechanism in mechanisms:
			# Baseline is default System.Text.Json run without options
			baseline = results["{} Default".format(title)]
			
			mean = results["{} {}".format(title, mechanism)]
			mean_as_str = str(mean)

			ratio_as_str = format(mean / float(baseline), '.2f')
			
			print "| {} | {} | {} |".format(
				mechanism + "".join([" "] * (test_col_width - len(mechanism))),
				mean_as_str + "".join([" "] * (mean_col_width - len(mean_as_str))),
				ratio_as_str + "".join([" "] * (ratio_col_width - len(ratio_as_str)))
			)

		print