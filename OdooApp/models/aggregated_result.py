from odoo import models, fields

class AggregatedResult(models.Model):
    _name = 'inventory_aggregator.aggregated_result'
    _description = 'Aggregated Result for Inventory Field'

    template_id = fields.Many2one('inventory_aggregator.template', string='Template', required=True, ondelete='cascade')
    field_name = fields.Char(string='Field Name', required=True)
    field_type = fields.Char(string='Field Type', required=True)
    average = fields.Float(string='Average')
    min_val = fields.Float(string='Minimum Value')
    max_val = fields.Float(string='Maximum Value')
    popular_text = fields.Char(string='Popular Text')
