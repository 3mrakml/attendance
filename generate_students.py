import pandas as pd
import random
from datetime import datetime, timedelta

def random_date(start, end):
    return start + timedelta(days=random.randint(0, int((end - start).days)))

names = ["أحمد", "محمد", "علي", "محمود", "عمر", "طارق", "سامي", "مصطفى", "حسن", "حسين",
         "فاطمة", "زينب", "مريم", "سارة", "منى", "هدى", "ليلى", "نور", "ياسمين", "ريهام"]
last_names = ["إبراهيم", "سليمان", "عبدالله", "يوسف", "عثمان", "توفيق", "النجار", "الحداد", "المصري", "منصور"]

start_date = datetime(2005, 1, 1)
end_date = datetime(2010, 12, 31)

grades = ["الصف الأول الثانوي", "الصف الثاني الثانوي", "الصف الثالث الثانوي"]

data = []

for grade in grades:
    for i in range(300):
        name = f"{random.choice(names)} {random.choice(last_names)} {random.choice(names)}"
        phone = f"01{random.choice(['0', '1', '2', '5'])}{random.randint(10000000, 99999999)}"
        dob = random_date(start_date, end_date).strftime('%Y-%m-%d')
        
        row = {
            "الاسم": name,
            "الهاتف": phone,
            "تاريخ الميلاد": dob,
            "السن": "",  # Age is optional if DOB is provided
            "الصف": grade
        }
        
        # Inject deliberate errors (roughly 10% of the rows)
        error_type = random.randint(1, 100)
        if error_type <= 2:
            row["الاسم"] = ""  # Missing Name
        elif error_type <= 4:
            row["الهاتف"] = ""  # Missing Phone
        elif error_type <= 6:
            row["الصف"] = "الصف العاشر"  # Invalid Grade
        elif error_type <= 8:
            row["تاريخ الميلاد"] = "not-a-date"  # Invalid DOB
            row["السن"] = "" # Both empty/invalid
            
        data.append(row)

# Add some exact duplicates
duplicate_row = {
    "الاسم": "مكرر متعمد",
    "الهاتف": "01000000000",
    "تاريخ الميلاد": "2006-05-05",
    "السن": "",
    "الصف": "الصف الأول الثانوي"
}
data.append(duplicate_row)
data.append(duplicate_row) # This second one will be flagged as duplicate

df = pd.DataFrame(data)
# Shuffle the dataframe
df = df.sample(frac=1).reset_index(drop=True)

df.to_excel("900_Students_Test.xlsx", index=False)
print("File generated successfully: 900_Students_Test.xlsx")
