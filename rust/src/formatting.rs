use std::io::Write;

use serde_json::Value;

/// Extract a non-null string field from a JSON object.
fn get_str<'a>(val: &'a Value, key: &str) -> Option<&'a str> {
    val.get(key).and_then(Value::as_str).filter(|s| !s.is_empty())
}

/// Extract a latitude or longitude value that may be stored as a number or string.
fn get_coord(val: &Value, key: &str) -> Option<String> {
    let field = val.get(key)?;
    if let Some(n) = field.as_f64() {
        return Some(n.to_string());
    }
    field
        .as_str()
        .filter(|s| !s.is_empty())
        .map(String::from)
}

/// Format the address fields of a record into a single comma-separated string.
///
/// Combines street, house number, extra address info, postal code, city, and country
/// into a human-readable address. Returns an empty string if no address fields are present.
pub fn format_address(record: &Value) -> String {
    let mut parts: Vec<String> = Vec::new();

    let street = get_str(record, "addressStreet");
    let number = get_str(record, "addressHouseNumber");
    match (street, number) {
        (Some(s), Some(n)) => parts.push(format!("{s} {n}")),
        (Some(s), None) => parts.push(s.to_string()),
        _ => {}
    }

    if let Some(additional) = get_str(record, "addressExtra") {
        parts.push(additional.to_string());
    }

    let post_code = get_str(record, "postCode");
    let city = get_str(record, "city");
    let country = get_str(record, "countryName").or_else(|| get_str(record, "country"));

    let locality_pieces: Vec<&str> = [post_code, city].into_iter().flatten().collect();
    let locality = locality_pieces.join(" ");
    if !locality.is_empty() {
        parts.push(locality);
    }

    if let Some(c) = country {
        parts.push(c.to_string());
    }

    parts.join(", ")
}

/// Extract formatted contact fields (phone, website, email) from a record.
///
/// Returns a vector of strings like `"Phone: +1234567890"`. Only fields present
/// in the record are included.
pub fn format_contact_fields(record: &Value) -> Vec<String> {
    let mut fields = Vec::new();

    let phone = get_str(record, "phone").or_else(|| get_str(record, "phoneOrMobile"));
    if let Some(p) = phone {
        fields.push(format!("Phone: {p}"));
    }
    if let Some(w) = get_str(record, "website") {
        fields.push(format!("Website: {w}"));
    }
    if let Some(e) = get_str(record, "email") {
        fields.push(format!("Email: {e}"));
    }

    fields
}

/// Write a formatted list of business records to the given writer.
///
/// This is the testable core of [`print_results`].
pub fn write_results(w: &mut impl Write, records: &[Value]) {
    for (i, record) in records.iter().enumerate() {
        let index = i + 1;
        let company = get_str(record, "companyName")
            .or_else(|| get_str(record, "businessName"))
            .unwrap_or("<Unknown>");
        let address = format_address(record);
        let address_display = if address.is_empty() {
            "<No address provided>"
        } else {
            &address
        };

        let _ = writeln!(w, "Result {index}:");
        if let Some(uid) = get_str(record, "uniqueID") {
            let _ = writeln!(w, "  UniqueID: {uid}");
        }
        let _ = writeln!(w, "  Company: {company}");
        let _ = writeln!(w, "  Address: {address_display}");

        for field in format_contact_fields(record) {
            let _ = writeln!(w, "  {field}");
        }

        let latitude = get_coord(record, "latitude");
        let longitude = get_coord(record, "longitude");
        if let (Some(lat), Some(lon)) = (&latitude, &longitude) {
            let _ = writeln!(w, "  Location: {lat}, {lon}");
            let _ = writeln!(
                w,
                "  OpenStreetMap: https://www.openstreetmap.org/?mlat={lat}&mlon={lon}"
            );
        }

        let activity = get_str(record, "internationalLabel01")
            .or_else(|| get_str(record, "altInternationalLabel01"));
        if let Some(a) = activity {
            let _ = writeln!(w, "  Activity: {a}");
        }

        let _ = writeln!(w);
    }
}

/// Print a formatted list of business records to stdout.
///
/// Each record is printed with its index, company name, address, contact fields,
/// geographic coordinates (with an OpenStreetMap link), and activity label.
pub fn print_results(records: &[Value]) {
    write_results(&mut std::io::stdout(), records);
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;

    // -----------------------------------------------------------------------
    // format_address
    // -----------------------------------------------------------------------

    #[test]
    fn test_format_address_full() {
        let record = json!({
            "addressStreet": "Main St",
            "addressHouseNumber": "42",
            "postCode": "10001",
            "city": "New York",
            "countryName": "United States",
        });
        let result = format_address(&record);
        assert!(result.contains("Main St 42"));
        assert!(result.contains("10001 New York"));
        assert!(result.contains("United States"));
    }

    #[test]
    fn test_format_address_street_without_number() {
        let record = json!({"addressStreet": "Broadway", "city": "LA"});
        let result = format_address(&record);
        assert!(result.contains("Broadway"));
        assert!(result.contains("LA"));
    }

    #[test]
    fn test_format_address_empty() {
        assert_eq!(format_address(&json!({})), "");
    }

    #[test]
    fn test_format_address_extra() {
        let record = json!({"addressStreet": "Elm", "addressExtra": "Suite 5"});
        let result = format_address(&record);
        assert!(result.contains("Suite 5"));
    }

    #[test]
    fn test_format_address_country_fallback() {
        let record = json!({"country": "US"});
        let result = format_address(&record);
        assert!(result.contains("US"));
    }

    // -----------------------------------------------------------------------
    // format_contact_fields
    // -----------------------------------------------------------------------

    #[test]
    fn test_format_contact_fields_all() {
        let record = json!({"phone": "123", "website": "example.com", "email": "a@b.c"});
        let fields = format_contact_fields(&record);
        assert!(fields.contains(&"Phone: 123".to_string()));
        assert!(fields.contains(&"Website: example.com".to_string()));
        assert!(fields.contains(&"Email: a@b.c".to_string()));
    }

    #[test]
    fn test_format_contact_fields_mobile_fallback() {
        let record = json!({"phoneOrMobile": "555"});
        let fields = format_contact_fields(&record);
        assert!(fields.contains(&"Phone: 555".to_string()));
    }

    #[test]
    fn test_format_contact_fields_empty() {
        assert!(format_contact_fields(&json!({})).is_empty());
    }

    // -----------------------------------------------------------------------
    // get_coord
    // -----------------------------------------------------------------------

    #[test]
    fn test_get_coord_numeric() {
        let val = json!({"lat": 37.35});
        assert_eq!(get_coord(&val, "lat").unwrap(), "37.35");
    }

    #[test]
    fn test_get_coord_string() {
        let val = json!({"lat": "37.35"});
        assert_eq!(get_coord(&val, "lat").unwrap(), "37.35");
    }

    #[test]
    fn test_get_coord_missing() {
        assert!(get_coord(&json!({}), "lat").is_none());
    }

    #[test]
    fn test_get_coord_empty_string() {
        let val = json!({"lat": ""});
        assert!(get_coord(&val, "lat").is_none());
    }

    // -----------------------------------------------------------------------
    // write_results / print_results
    // -----------------------------------------------------------------------

    #[test]
    fn test_write_results_full_record() {
        let records = vec![json!({
            "companyName": "Nvidia",
            "addressStreet": "Main",
            "addressHouseNumber": "1",
            "postCode": "95050",
            "city": "Santa Clara",
            "countryName": "US",
            "uniqueID": "U1",
            "phone": "555-0100",
            "website": "nvidia.com",
            "email": "info@nvidia.com",
            "latitude": 37.35,
            "longitude": -121.95,
            "internationalLabel01": "Semiconductors",
        })];
        let mut buf = Vec::new();
        write_results(&mut buf, &records);
        let out = String::from_utf8(buf).unwrap();
        assert!(out.contains("Nvidia"));
        assert!(out.contains("UniqueID: U1"));
        assert!(out.contains("Phone: 555-0100"));
        assert!(out.contains("Website: nvidia.com"));
        assert!(out.contains("Email: info@nvidia.com"));
        assert!(out.contains("37.35"));
        assert!(out.contains("OpenStreetMap"));
        assert!(out.contains("Semiconductors"));
    }

    #[test]
    fn test_write_results_minimal_record() {
        let records = vec![json!({})];
        let mut buf = Vec::new();
        write_results(&mut buf, &records);
        let out = String::from_utf8(buf).unwrap();
        assert!(out.contains("<Unknown>"));
        assert!(out.contains("<No address provided>"));
    }

    #[test]
    fn test_write_results_alt_activity_label() {
        let records = vec![json!({"altInternationalLabel01": "Electronics"})];
        let mut buf = Vec::new();
        write_results(&mut buf, &records);
        let out = String::from_utf8(buf).unwrap();
        assert!(out.contains("Activity: Electronics"));
    }

    #[test]
    fn test_write_results_business_name_fallback() {
        let records = vec![json!({"businessName": "Acme Corp"})];
        let mut buf = Vec::new();
        write_results(&mut buf, &records);
        let out = String::from_utf8(buf).unwrap();
        assert!(out.contains("Acme Corp"));
    }
}
