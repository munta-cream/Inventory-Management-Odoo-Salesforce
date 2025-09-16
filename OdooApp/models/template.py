from odoo import models, fields, api
import requests
import json

class Template(models.Model):
    _name = 'inventory_aggregator.template'
    _description = 'Template for Aggregated Inventory Data'

    name = fields.Char(string='Name', required=True)
    description = fields.Text(string='Description')
    api_token = fields.Char(string='API Token', required=True)
    api_url = fields.Char(string='API URL', required=True)
    aggregated_result_ids = fields.One2many('inventory_aggregator.aggregated_result', 'template_id', string='Aggregated Results')

    def import_data(self):
        headers = {'X-API-Token': self.api_token}
        try:
            response = requests.get(self.api_url, headers=headers)
            response.raise_for_status()
            data = response.json()
        except Exception as e:
            raise Exception(f"Failed to fetch data from API: {e}")

        # Clear existing aggregated results
        self.aggregated_result_ids.unlink()

        for inventory in data:
            for field in inventory.get('fields', []):
                self.env['inventory_aggregator.aggregated_result'].create({
                    'template_id': self.id,
                    'field_name': field.get('title'),
                    'field_type': field.get('type'),
                    'average': field.get('aggregation', {}).get('average'),
                    'min_val': field.get('aggregation', {}).get('min'),
                    'max_val': field.get('aggregation', {}).get('max'),
                    'popular_text': field.get('aggregation', {}).get('popularAnswer'),
                })
